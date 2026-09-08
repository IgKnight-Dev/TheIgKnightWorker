using AutoMapper;
using Microsoft.AspNetCore.Http;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Application.Helpers;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.ResumeTemplates;
using TheIgKnightWorker.Domain.Entities;
using TheIgKnightWorker.Domain.UnitOfWork;
using Document = TheIgKnightWorker.Domain.Entities.Document;

namespace TheIgKnightWorker.Application.Services;

public class ResumeService : IResumeService
{
    private const string DefaultTemplateKey = "professional";
    private const long MaxResumeFileSize = 10 * 1024 * 1024;

    public readonly IUnitOfWork _unitOfWork;

    private readonly IFileService _fileService;
    private readonly IDocumentService _documentService;
    private readonly IResumeHtmlBuilder _htmlBuilder;
    private readonly IResumePdfGenerator _pdfGenerator;
    private readonly ResumeTemplateFactory _templateFactory;
    private readonly IMapper _mapper;

    public ResumeService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IFileService fileService,
        IDocumentService documentService,
        IResumeHtmlBuilder htmlBuilder,
        IResumePdfGenerator pdfGenerator)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _documentService = documentService;
        _htmlBuilder = htmlBuilder;
        _pdfGenerator = pdfGenerator;

        _templateFactory = new ResumeTemplateFactory();
    }

    public async Task<ResumeGenerationResponseDto> GenerateResumeAsync(
        CreatePortfolioDto portfolioDto,
        ResumeGenerationRequestDto requestDto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(portfolioDto);

        if (userId == Guid.Empty)
        {
            return CreateFailureResponse(
                DefaultTemplateKey,
                "User ID is required.");
        }

        Portfolio portfolio =
            _mapper.Map<Portfolio>(portfolioDto);

        ArgumentNullException.ThrowIfNull(portfolio);

        requestDto ??= new ResumeGenerationRequestDto();

        var templateKey =
            SelectTemplateKey(portfolio.MetaData);

        ResumeTemplateModel template;

        try
        {
            template =
                _templateFactory.GetTemplate(templateKey);
        }
        catch
        {
            return CreateFailureResponse(
                templateKey,
                "The selected resume template could not be found.");
        }

        string html;

        try
        {
            var skillIds =
                portfolioDto.SkillIds
                    .Distinct()
                    .ToList();

            var skills =
                await _unitOfWork
                    .SkillRepository
                    .GetSkillsByIdsAsync(skillIds);

            html =
                _htmlBuilder.Build(
                    template,
                    portfolio,
                    skills.ToList());
        }
        catch
        {
            return CreateFailureResponse(
                templateKey,
                "The resume could not be prepared from your portfolio data.");
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            return CreateFailureResponse(
                templateKey,
                "The resume template produced empty content.");
        }

        byte[] pdfBytes;

        try
        {
            pdfBytes =
                await _pdfGenerator.GenerateAsync(
                    html,
                    cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            return CreateFailureResponse(
                templateKey,
                ex.Message);
        }
        catch (Exception ex)
        {
            return CreateFailureResponse(
                templateKey,
                ex.Message);
        }

        if (pdfBytes == null ||
            pdfBytes.Length == 0)
        {
            return CreateFailureResponse(
                templateKey,
                "The generated resume was empty. Please try again.");
        }

        if (pdfBytes.Length > MaxResumeFileSize)
        {
            return CreateFailureResponse(
                templateKey,
                "The generated resume exceeds the allowed file size.");
        }

        if (!IsPdf(pdfBytes))
        {
            return CreateFailureResponse(
                templateKey,
                "The generated resume is invalid. Please try again.");
        }

        var fileName =
            BuildResumeFileName(
                portfolio,
                templateKey);

        FileUploadResponseDto uploadResult;

        try
        {
            await using var stream =
                new MemoryStream(pdfBytes);

            var formFile =
                new MemoryFormFile(
                    stream,
                    "file",
                    fileName,
                    "application/pdf");

            uploadResult =
                await _fileService.UploadAsync(formFile);
        }
        catch
        {
            return CreateFailureResponse(
                templateKey,
                "The resume was generated but could not be stored. Please try again.");
        }

        if (uploadResult == null ||
            string.IsNullOrWhiteSpace(uploadResult.FileUrl))
        {
            return CreateFailureResponse(
                templateKey,
                "The generated resume could not be stored.");
        }

        var previousDocumentId =
            portfolio.MetaData?.ResumeDocumentId;

        Guid documentId;

        try
        {
            var document =
                new Document
                {
                    DocumentId = Guid.NewGuid(),
                    PortfolioId = portfolio.Id,
                    FileName = uploadResult.FileName,
                    FileUrl = uploadResult.FileUrl,
                    FileType =
                        string.IsNullOrWhiteSpace(
                            uploadResult.FileType)
                            ? "application/pdf"
                            : uploadResult.FileType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };

            documentId =
                await _unitOfWork
                    .DocumentRepository
                    .AddDocumentAsync(document);
        }
        catch
        {
            return CreateFailureResponse(
                templateKey,
                "The generated resume could not be registered.");
        }

        try
        {
            portfolio.MetaData ??=
                new PortfolioMetaData();

            portfolio.MetaData.ResumeDocumentId =
                documentId;

            portfolio.MetaData.ResumeUrl =
                uploadResult.FileUrl;

            portfolio.MetaData.ResumeFileName =
                uploadResult.FileName;
        }
        catch
        {
            return CreateFailureResponse(
                templateKey,
                "The resume was generated but could not be attached to your portfolio.");
        }

        if (previousDocumentId.HasValue &&
            previousDocumentId.Value != Guid.Empty &&
            previousDocumentId.Value != documentId)
        {
            try
            {
                await _documentService
                    .DeleteDocumentAsync(
                        previousDocumentId.Value);
            }
            catch
            {
            }
        }

        return new ResumeGenerationResponseDto
        {
            DocumentId = documentId,
            FileUrl = uploadResult.FileUrl,
            FileName = uploadResult.FileName,
            TemplateKey = templateKey,
            ResumeType = "Generated",
            IsSuccess = true,
            Message = "Resume generated successfully."
        };
    }

    private static string SelectTemplateKey(
        PortfolioMetaData data)
    {
        var hasExperience =
            data.Experience != null &&
            data.Experience.Any();

        var hasProjects =
            data.Projects != null &&
            data.Projects.Any();

        if (hasExperience && hasProjects)
        {
            return "professional";
        }

        if (hasExperience && !hasProjects)
        {
            return "experienced";
        }

        if (!hasExperience && hasProjects)
        {
            return "fresher";
        }

        return "student";
    }

    private static string BuildResumeFileName(
        Portfolio portfolio,
        string templateKey)
    {
        var fullName =
            portfolio.MetaData?
                .BasicInfo?
                .FullName;

        var safeName =
            SanitizeFileName(fullName);

        if (string.IsNullOrWhiteSpace(
            safeName))
        {
            safeName = "resume";
        }

        return
            $"{safeName}-resume-{templateKey}.pdf";
    }

    private static string SanitizeFileName(
        string? fileName)
    {
        if (string.IsNullOrWhiteSpace(
            fileName))
        {
            return string.Empty;
        }

        var invalidCharacters =
            Path.GetInvalidFileNameChars();

        var result =
            new string(
                fileName
                    .Where(c =>
                        !invalidCharacters.Contains(c))
                    .ToArray());

        result =
            result.Trim();

        result =
            string.Join(
                "-",
                result.Split(
                    new[]
                    {
                        ' ',
                        '\t',
                        '\r',
                        '\n'
                    },
                    StringSplitOptions.RemoveEmptyEntries));

        return result;
    }

    private static bool IsAllowedResumeFile(
        IFormFile file)
    {
        var extension =
            Path.GetExtension(
                file.FileName)
                .ToLowerInvariant();

        if (extension != ".pdf")
            return false;

        var contentType =
            file.ContentType?
                .ToLowerInvariant();

        return
            contentType == "application/pdf" ||
            string.IsNullOrWhiteSpace(
                contentType);
    }

    private static bool IsPdf(
        byte[] bytes)
    {
        if (bytes == null ||
            bytes.Length < 5)
        {
            return false;
        }

        return
            bytes[0] == (byte)'%' &&
            bytes[1] == (byte)'P' &&
            bytes[2] == (byte)'D' &&
            bytes[3] == (byte)'F' &&
            bytes[4] == (byte)'-';
    }

    private static ResumeGenerationResponseDto
        CreateFailureResponse(
            string templateKey,
            string message)
    {
        return new ResumeGenerationResponseDto
        {
            TemplateKey = templateKey,
            ResumeType = "Generated",
            IsSuccess = false,
            Message = message
        };
    }
}
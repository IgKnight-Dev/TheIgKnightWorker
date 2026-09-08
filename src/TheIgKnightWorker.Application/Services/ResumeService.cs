using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright;
using System.Net;
using System.Text;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Application.Helpers;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.ResumeTemplates;
using TheIgKnightWorker.Domain.Common.ContextUser;
using TheIgKnightWorker.Domain.Entities;
using TheIgKnightWorker.Domain.UnitOfWork;
using Document = TheIgKnightWorker.Domain.Entities.Document;

namespace TheIgKnightWorker.Application.Services;

public class ResumeService : IResumeService, IAsyncDisposable
{
    private const string DefaultTemplateKey = "professional";
    private const long MaxResumeFileSize = 10 * 1024 * 1024;

    public readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;
    private readonly IFileService _fileService;
    private readonly IDocumentService _documentService;
    private readonly ResumeTemplateFactory _templateFactory;

    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private readonly IMapper _mapper;
    private static readonly SemaphoreSlim _browserLock = new(1, 1);

    public ResumeService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IRequestContext requestContext,
        IFileService fileService,
        IDocumentService documentService)
    {
        this._mapper = mapper;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
        _fileService = fileService;
        _documentService = documentService;

        _templateFactory = new ResumeTemplateFactory();
    }

    public async Task<ResumeGenerationResponseDto> GenerateResumeAsync(
        CreatePortfolioDto portfolioDto,
        ResumeGenerationRequestDto requestDto)
    {
        Portfolio portfolio = this._mapper.Map<Portfolio>(portfolioDto);
        ArgumentNullException.ThrowIfNull(portfolio);

        requestDto ??= new ResumeGenerationRequestDto();

        var templateKey = SelectTemplateKey(portfolio.MetaData);

        ResumeTemplateModel template;

        try
        {
            template =
                _templateFactory.GetTemplate(templateKey);
        }
        catch (Exception ex)
        {
            return CreateFailureResponse(
                templateKey,
                "The selected resume template could not be found.");
        }

        string html;

        try
        {

            var skillIds = portfolioDto.SkillIds.Select(id => id).Distinct().ToList();

            var skills = await _unitOfWork.SkillRepository.GetSkillsByIdsAsync(skillIds);

            html = RenderTemplate(
                template,
                portfolio, skills.ToList());
        }
        catch (Exception ex)
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

        var pdfGenerationStopwatch = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine(
            $"[RESUME-PDF] Starting PDF generation for portfolio {portfolio.Id} at {DateTime.UtcNow:O}");

        try
        {
            var pdfStopwatch = System.Diagnostics.Stopwatch.StartNew();

            Console.WriteLine(
                $"[RESUME-PDF] Calling ConvertHtmlToPdfBytesAsync for portfolio {portfolio.Id}");

            pdfBytes =
                await ConvertHtmlToPdfBytesAsync(
                    html,
                    portfolio.Id);

            pdfStopwatch.Stop();

            Console.WriteLine(
                $"[RESUME-PDF] ConvertHtmlToPdfBytesAsync completed for portfolio {portfolio.Id} " +
                $"in {pdfStopwatch.ElapsedMilliseconds} ms ({pdfStopwatch.Elapsed.TotalSeconds:F2}s)");
        }
        catch (OperationCanceledException ex)
        {
            pdfGenerationStopwatch.Stop();

            Console.WriteLine(
                $"[RESUME-PDF] PDF generation CANCELLED for portfolio {portfolio.Id} " +
                $"after {pdfGenerationStopwatch.ElapsedMilliseconds} ms ({pdfGenerationStopwatch.Elapsed.TotalSeconds:F2}s)");

            return CreateFailureResponse(
                templateKey,
                ex.Message);
        }
        catch (Exception ex)
        {
            pdfGenerationStopwatch.Stop();

            Console.WriteLine(
                $"[RESUME-PDF] PDF generation FAILED for portfolio {portfolio.Id} " +
                $"after {pdfGenerationStopwatch.ElapsedMilliseconds} ms ({pdfGenerationStopwatch.Elapsed.TotalSeconds:F2}s)");

            return CreateFailureResponse(
                templateKey,
                ex.Message);
        }

        pdfGenerationStopwatch.Stop();

        Console.WriteLine(
            $"[RESUME-PDF] PDF generation completed for portfolio {portfolio.Id} " +
            $"in {pdfGenerationStopwatch.ElapsedMilliseconds} ms ({pdfGenerationStopwatch.Elapsed.TotalSeconds:F2}s)");

        if (pdfBytes.Length == 0)
        {
            Console.WriteLine(
                $"[RESUME-PDF] ERROR: Empty PDF returned for portfolio {portfolio.Id}");

            return CreateFailureResponse(
                templateKey,
                "The generated resume was empty. Please try again.");
        }

        Console.WriteLine(
            $"[RESUME-PDF] Generated PDF size for portfolio {portfolio.Id}: " +
            $"{pdfBytes.Length:N0} bytes");

        if (!IsPdf(pdfBytes))
        {
            Console.WriteLine(
                $"[RESUME-PDF] ERROR: Invalid PDF returned for portfolio {portfolio.Id}");

            return CreateFailureResponse(
                templateKey,
                "The generated resume is invalid. Please try again.");
        }

        var fileName =
            BuildResumeFileName(
                portfolio,
                templateKey);

        FileUploadResponseDto uploadResult;

        var uploadStopwatch = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine(
            $"[RESUME-PDF] Starting PDF upload for portfolio {portfolio.Id}");

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

            uploadStopwatch.Stop();

            Console.WriteLine(
                $"[RESUME-PDF] PDF upload completed for portfolio {portfolio.Id} " +
                $"in {uploadStopwatch.ElapsedMilliseconds} ms " +
                $"({uploadStopwatch.Elapsed.TotalSeconds:F2}s)");
        }
        catch (Exception ex)
        {
            uploadStopwatch.Stop();

            Console.WriteLine(
                $"[RESUME-PDF] PDF upload FAILED for portfolio {portfolio.Id} " +
                $"after {uploadStopwatch.ElapsedMilliseconds} ms " +
                $"({uploadStopwatch.Elapsed.TotalSeconds:F2}s)");

            return CreateFailureResponse(
                templateKey,
                "The resume was generated but could not be stored. Please try again.");
        }

        if (uploadResult == null ||
            string.IsNullOrWhiteSpace(uploadResult.FileUrl))
        {
            Console.WriteLine(
                $"[RESUME-PDF] ERROR: Upload returned invalid response for portfolio {portfolio.Id}");

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
                        string.IsNullOrWhiteSpace(uploadResult.FileType)
                            ? "application/pdf"
                            : uploadResult.FileType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = this.GetUserIdentifier()
                };

            documentId =
                await _unitOfWork
                    .DocumentRepository
                    .AddDocumentAsync(document);
        }
        catch (Exception ex)
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
        catch (Exception ex)
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
            catch (Exception ex)
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

    private static string RenderTemplate(
        ResumeTemplateModel template,
        Portfolio portfolio, List<Skill> skillList)
    {
        if (template == null)
        {
            throw new ArgumentNullException(
                nameof(template));
        }

        if (string.IsNullOrWhiteSpace(
            template.HtmlTemplatePath))
        {
            throw new InvalidOperationException(
                "Resume template HTML path is empty.");
        }

        if (!File.Exists(
            template.HtmlTemplatePath))
        {
            throw new FileNotFoundException(
                $"Resume template was not found: {template.HtmlTemplatePath}",
                template.HtmlTemplatePath);
        }

        var html =
            File.ReadAllText(
                template.HtmlTemplatePath,
                Encoding.UTF8);

        var metadata =
            portfolio.MetaData;

        var basicInfo =
            metadata?.BasicInfo;

        var fullName =
            HtmlEncode(
                string.IsNullOrWhiteSpace(
                    basicInfo?.FullName)
                    ? "Your Name"
                    : basicInfo.FullName);

        var headline =
            HtmlEncode(
                string.IsNullOrWhiteSpace(
                    basicInfo?.Headline)
                    ? "Developer"
                    : basicInfo.Headline);

        var summary =
            HtmlEncode(
                metadata?.Summary ?? string.Empty);

        var skills =
            BuildSkillsHtml(metadata, skillList);

        var experience =
            BuildExperienceHtml(metadata);

        var education =
            BuildEducationHtml(metadata);

        var projects =
            BuildProjectsHtml(metadata);

        var achievements =
            BuildAchievementsHtml(metadata);

        var achievementsSection = metadata.BuildAchievementsSectionHtml(template.TemplateKey, achievements);

        var contact =
            BuildContactHtml(basicInfo);

        var links =
            BuildLinksHtml(basicInfo);

        var result = html
            .Replace("{{Location}}", metadata.BasicInfo.Location)
            .Replace("{{Email}}", metadata.BasicInfo.Email)
            .Replace("{{Phone}}", metadata.BasicInfo.Phone)
            .Replace(
                "{{FullName}}",
                fullName,
                StringComparison.Ordinal)
            .Replace(
                "{{Headline}}",
                headline,
                StringComparison.Ordinal)
            .Replace(
                "{{Summary}}",
                summary,
                StringComparison.Ordinal)
            .Replace(
                "{{Skills}}",
                skills,
                StringComparison.Ordinal)
            .Replace(
                "{{Experience}}",
                experience,
                StringComparison.Ordinal)
            .Replace(
                "{{Education}}",
                education,
                StringComparison.Ordinal)
            .Replace(
                "{{Projects}}",
                projects,
                StringComparison.Ordinal)
            .Replace(
                "{{Contact}}",
                contact,
                StringComparison.Ordinal)
            .Replace(
                "{{Links}}",
                links,
                StringComparison.Ordinal);

        result = result.Replace(
            "<!-- ACHIEVEMENTS_INJECTION_POINT -->",
            achievementsSection,
            StringComparison.Ordinal);

        return result;
    }

    private static string BuildSkillsHtml(PortfolioMetaData? metadata, List<Skill> skills)
    {
        if (skills == null || skills.Count == 0)
        {
            return string.Empty;
        }

        var skillMap = skills.Select(skill => skill.Name);

        var items = skillMap
            .Select(skill =>
            {
                return
                    $"<span class=\"resume-skill\">" +
                    $"{HtmlEncode(skill)}" +
                    $"</span>";
            })
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(
            Environment.NewLine,
            items);
    }

    private static string BuildExperienceHtml(
        PortfolioMetaData? metadata)
    {
        if (metadata?.Experience == null ||
            !metadata.Experience.Any())
        {
            return string.Empty;
        }
        var builder =
            new StringBuilder();
        foreach (var item in metadata.Experience)
        {
            if (item == null)
                continue;
            builder.AppendLine("""
            <div style="
                margin-bottom:11px;
                page-break-inside:avoid;
            ">
            """);
            if (!string.IsNullOrWhiteSpace(
                item.Role))
            {
                builder.AppendLine($"""
                <div style="
                    font-size:10pt;
                    font-weight:700;
                    color:#111827;
                ">
                    {HtmlEncode(item.Role)}
                </div>
                """);
            }
            if (!string.IsNullOrWhiteSpace(
                item.Company))
            {
                builder.AppendLine($"""
                <div style="
                    margin-top:2px;
                    font-size:9.3pt;
                    font-weight:600;
                    color:#374151;
                ">
                    {HtmlEncode(item.Company)}
                </div>
                """);
            }
            var location =
                !string.IsNullOrWhiteSpace(
                    item.Location)
                    ? HtmlEncode(item.Location)
                    : string.Empty;
            var startDate =
                !string.IsNullOrWhiteSpace(
                    item.StartDate)
                    ? HtmlEncode(item.StartDate)
                    : string.Empty;
            var endDate =
                !string.IsNullOrWhiteSpace(
                    item.EndDate)
                    ? HtmlEncode(item.EndDate)
                    : string.Empty;
            var hasLocation =
                !string.IsNullOrWhiteSpace(
                    location);
            var hasStartDate =
                !string.IsNullOrWhiteSpace(
                    startDate);
            var hasEndDate =
                !string.IsNullOrWhiteSpace(
                    endDate);
            if (hasLocation ||
                hasStartDate ||
                hasEndDate)
            {
                builder.AppendLine("""
                <div style="
                    margin-top:2px;
                    font-size:8.8pt;
                    color:#6b7280;
                ">
                """);
                if (hasLocation)
                    builder.Append(location);
                if (hasLocation &&
                    (hasStartDate ||
                     hasEndDate))
                {
                    builder.Append(
                        " &nbsp; | &nbsp; ");
                }
                if (hasStartDate)
                    builder.Append(startDate);
                if (hasStartDate &&
                    hasEndDate)
                {
                    builder.Append(" - ");
                }
                if (hasEndDate)
                    builder.Append(endDate);
                builder.AppendLine("</div>");
            }
            if (!string.IsNullOrWhiteSpace(
                item.Description))
            {
                builder.AppendLine($"""
                <div style="
                    margin-top:4px;
                ">
                    {FormatMultilineDescriptionHtml(item.Description)}
                </div>
                """);
            }
            builder.AppendLine("</div>");
        }
        return builder.ToString();
    }

    private static string FormatMultilineDescriptionHtml(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var lines = description
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var builder = new StringBuilder();
        var inList = false;

        foreach (var line in lines)
        {
            var isBullet = line.StartsWith("•");

            if (isBullet && !inList)
            {
                builder.Append("<ul style=\"margin:4px 0; padding-left:16px;\">");
                inList = true;
            }
            else if (!isBullet && inList)
            {
                builder.Append("</ul>");
                inList = false;
            }

            var text = isBullet ? line.TrimStart('•').Trim() : line;
            var encoded = HtmlEncode(text);

            builder.Append(isBullet
                ? $"<li style=\"margin-bottom:2px; font-size:9pt; line-height:1.45; color:#374151;\">{encoded}</li>"
                : $"<p style=\"margin:0 0 4px 0; font-size:9pt; line-height:1.45; color:#374151;\">{encoded}</p>");
        }

        if (inList)
            builder.Append("</ul>");

        return builder.ToString();
    }

    private static string BuildEducationHtml(
    PortfolioMetaData? metadata)
    {
        if (metadata?.Education == null ||
            !metadata.Education.Any())
        {
            return string.Empty;
        }

        var builder =
            new StringBuilder();

        foreach (var item in metadata.Education)
        {
            if (item == null)
                continue;

            builder.AppendLine("""
            <div style="
                margin-bottom:10px;
                page-break-inside:avoid;
            ">
            """);

            if (!string.IsNullOrWhiteSpace(
                item.Degree))
            {
                builder.AppendLine($"""
                <div style="
                    font-size:10pt;
                    font-weight:700;
                    color:#111827;
                ">
                    {HtmlEncode(item.Degree)}
                </div>
                """);
            }

            var startYear =
                item.StartYear > 0
                    ? item.StartYear.ToString()
                    : string.Empty;

            var endYear =
                item.EndYear.HasValue &&
                item.EndYear.Value > 0
                    ? item.EndYear.Value.ToString()
                    : string.Empty;

            var educationDates =
                !string.IsNullOrWhiteSpace(startYear) &&
                !string.IsNullOrWhiteSpace(endYear)
                    ? $"{startYear} – {endYear}"
                    : !string.IsNullOrWhiteSpace(startYear)
                        ? $"{startYear} – Present"
                        : endYear;

            if (!string.IsNullOrWhiteSpace(
                item.Institution) ||
                !string.IsNullOrWhiteSpace(
                    educationDates))
            {
                builder.AppendLine($"""
                <div style="
                    margin-top:2px;
                    display:flex;
                    justify-content:space-between;
                    align-items:baseline;
                    gap:10px;
                    font-size:9.2pt;
                ">
                    <span style="
                        color:#374151;
                    ">
                        {HtmlEncode(item.Institution ?? string.Empty)}
                    </span>

                    <span style="
                        color:#6b7280;
                        font-size:8.5pt;
                        font-weight:600;
                        white-space:nowrap;
                    ">
                        {HtmlEncode(educationDates)}
                    </span>
                </div>
                """);
            }

            builder.AppendLine("</div>");
        }

        return builder.ToString();
    }

    private static string BuildAchievementsHtml(
    PortfolioMetaData? metadata)
    {
        if (metadata?.Achievements == null ||
            !metadata.Achievements.Any())
        {
            return string.Empty;
        }

        var builder =
            new StringBuilder();

        foreach (var item in metadata.Achievements)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.Title) &&
                string.IsNullOrWhiteSpace(item.Description) &&
                string.IsNullOrWhiteSpace(item.Organization) &&
                string.IsNullOrWhiteSpace(item.Date) &&
                string.IsNullOrWhiteSpace(item.Link))
            {
                continue;
            }

            builder.AppendLine("""
            <div style="
                margin-bottom:12px;
                page-break-inside:avoid;
            ">
            """);

            if (!string.IsNullOrWhiteSpace(
                item.Title))
            {
                builder.AppendLine($"""
                <div style="
                    font-size:9pt;
                    font-weight:700;
                    color:#111827;
                    line-height:1.4;
                ">
                    {HtmlEncode(item.Title)}
                </div>
                """);
            }

            if (!string.IsNullOrWhiteSpace(
                    item.Organization) ||
                !string.IsNullOrWhiteSpace(
                    item.Date))
            {
                builder.AppendLine("""
                <div style="
                    margin-top:2px;
                    font-size:8.7pt;
                    color:#6b7280;
                ">
                """);

                if (!string.IsNullOrWhiteSpace(
                    item.Organization))
                {
                    builder.AppendLine(
                        HtmlEncode(item.Organization));
                }

                if (!string.IsNullOrWhiteSpace(
                    item.Organization) &&
                    !string.IsNullOrWhiteSpace(
                        item.Date))
                {
                    builder.Append(" &nbsp;|&nbsp; ");
                }

                if (!string.IsNullOrWhiteSpace(
                    item.Date))
                {
                    builder.Append(
                        HtmlEncode(item.Date));
                }

                builder.AppendLine("</div>");
            }

            if (!string.IsNullOrWhiteSpace(
                item.Description))
            {
                builder.AppendLine($"""
                <div style="
                    margin-top:4px;
                    font-size:9pt;
                    line-height:1.5;
                    color:#374151;
                ">
                    {HtmlEncode(item.Description)}
                </div>
                """);
            }

            if (!string.IsNullOrWhiteSpace(
                item.Link))
            {
                var encodedLink =
                    HtmlEncode(item.Link);

                builder.AppendLine($"""
                <div style="
                    margin-top:4px;
                    font-size:8.4pt;
                ">
                    <a
                        href="{encodedLink}"
                        target="_blank"
                        rel="noopener noreferrer"
                        style="
                            color:#6d28d9;
                            text-decoration:none;
                            font-weight:600;
                        ">
                        View Achievement
                    </a>
                </div>
                """);
            }

            builder.AppendLine("</div>");
        }

        return builder.ToString();
    }

    private static string BuildProjectsHtml(
        PortfolioMetaData? metadata)
    {
        if (metadata?.Projects == null ||
            !metadata.Projects.Any())
        {
            return string.Empty;
        }

        var builder =
            new StringBuilder();

        foreach (var item in metadata.Projects)
        {
            if (item == null)
                continue;

            builder.AppendLine("""
                <div style="
                    margin-bottom:11px;
                    page-break-inside:avoid;
                ">
                """);

            if (!string.IsNullOrWhiteSpace(
                item.Name))
            {
                builder.AppendLine($"""
                    <div style="
                        font-size:10pt;
                        font-weight:700;
                        color:#111827;
                    ">
                        {HtmlEncode(item.Name)}
                    </div>
                    """);
            }

            if (!string.IsNullOrWhiteSpace(
                item.Description))
            {
                builder.AppendLine($"""
                    <div style="
                        margin-top:3px;
                        font-size:9pt;
                        line-height:1.45;
                        color:#374151;
                    ">
                        {HtmlEncode(item.Description)}
                    </div>
                    """);
            }

            var liveUrl =
                NormalizeResumeUrl(
                    item.LiveUrl);

            var repositoryUrl =
                NormalizeResumeUrl(
                    item.RepositoryUrl);

            var hasLiveUrl =
                !string.IsNullOrWhiteSpace(
                    liveUrl);

            var hasRepositoryUrl =
                !string.IsNullOrWhiteSpace(
                    repositoryUrl);

            if (hasLiveUrl ||
                hasRepositoryUrl)
            {
                builder.AppendLine("""
                    <div style="
                        margin-top:4px;
                        font-size:8.5pt;
                    ">
                    """);

                if (hasLiveUrl)
                {
                    builder.AppendLine(
                        $"<a href=\"{HtmlAttributeEncode(liveUrl)}\" " +
                        "style=\"color:#2563eb; text-decoration:none;\">" +
                        "Live Project</a>");
                }

                if (hasLiveUrl &&
                    hasRepositoryUrl)
                {
                    builder.AppendLine(
                        "<span style=\"color:#9ca3af;\">" +
                        "&nbsp; | &nbsp;" +
                        "</span>");
                }

                if (hasRepositoryUrl)
                {
                    builder.AppendLine(
                        $"<a href=\"{HtmlAttributeEncode(repositoryUrl)}\" " +
                        "style=\"color:#2563eb; text-decoration:none;\">" +
                        "Repository</a>");
                }

                builder.AppendLine("</div>");
            }

            if (item.Highlights != null &&
                item.Highlights.Any(
                    h => !string.IsNullOrWhiteSpace(h)))
            {
                builder.AppendLine("""
                    <div style="
                        margin-top:5px;
                        font-size:8.9pt;
                        line-height:1.45;
                        color:#374151;
                    ">
                    """);

                foreach (var highlight
                    in item.Highlights)
                {
                    if (string.IsNullOrWhiteSpace(
                        highlight))
                    {
                        continue;
                    }

                    builder.AppendLine($"""
                        <div style="
                            margin-bottom:2px;
                        ">
                            • {HtmlEncode(highlight)}
                        </div>
                        """);
                }

                builder.AppendLine("</div>");
            }

            builder.AppendLine("</div>");
        }

        return builder.ToString();
    }

    private static string BuildContactHtml(
    dynamic? basicInfo)
    {
        if (basicInfo == null)
            return string.Empty;

        var values = new List<string>();

        void AddValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            values.Add(value);
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(
                (string?)basicInfo.Email))
            {
                var email =
                    ((string)basicInfo.Email).Trim();

                if (IsValidEmail(email))
                {
                    AddValue(
                        $"<a href=\"mailto:{HtmlAttributeEncode(email)}\" " +
                        "style=\"color:#374151; text-decoration:none;\">" +
                        $"{HtmlEncode(email)}</a>");
                }
            }
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(
                (string?)basicInfo.Phone))
            {
                var phone =
                    ((string)basicInfo.Phone).Trim();

                var phoneHref =
                    NormalizePhoneForLink(phone);

                if (!string.IsNullOrWhiteSpace(
                    phoneHref))
                {
                    AddValue(
                        $"<a href=\"tel:{HtmlAttributeEncode(phoneHref)}\" " +
                        "style=\"color:#374151; text-decoration:none;\">" +
                        $"{HtmlEncode(phone)}</a>");
                }
                else
                {
                    AddValue(
                        $"<span>{HtmlEncode(phone)}</span>");
                }
            }
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(
                (string?)basicInfo.Location))
            {
                AddValue(
                    $"<span>{HtmlEncode((string)basicInfo.Location)}</span>");
            }
        }
        catch
        {
        }

        if (values.Count == 0)
            return string.Empty;

        return $"""
        <span style="white-space:nowrap;">
            {string.Join("&nbsp; | &nbsp;", values)}
        </span>
        """;
    }

    private static string BuildLinksHtml(
        dynamic? basicInfo)
    {
        if (basicInfo == null)
            return string.Empty;

        var builder =
            new StringBuilder();

        try
        {
            if (basicInfo.Links == null)
                return string.Empty;

            var first = true;

            foreach (var link in basicInfo.Links)
            {
                if (link == null)
                    continue;

                string? url = null;
                string? label = null;

                try
                {
                    url = link.Url;
                }
                catch
                {
                }

                if (string.IsNullOrWhiteSpace(url))
                    continue;

                var normalizedUrl =
                    NormalizeResumeUrl(url);

                if (string.IsNullOrWhiteSpace(
                    normalizedUrl))
                {
                    continue;
                }

                try
                {

                    label =
                        link.Type.GetDescription();
                }
                catch
                {
                    try
                    {
                        label = link.Type.ToString();
                    }
                    catch
                    {
                        label = "Link";
                    }
                }

                if (!first)
                {
                    builder.Append(
                        "<span style=\"color:#9ca3af;\">" +
                        "&nbsp; | &nbsp;" +
                        "</span>");
                }

                builder.Append(
                    $"<a href=\"{HtmlAttributeEncode(normalizedUrl)}\" " +
                    "style=\"color:#2563eb; text-decoration:none;\">" +
                    $"{HtmlEncode(label)}</a>");

                first = false;
            }
        }
        catch
        {

        }

        return builder.ToString();
    }

    private static string NormalizeResumeUrl(
        string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim();

        if (!url.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            url =
                "https://" + url;
        }

        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out var uri))
        {
            return string.Empty;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return string.Empty;
        }

        return uri.ToString();
    }

    private static string NormalizePhoneForLink(
        string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var value =
            phone.Trim();

        var builder =
            new StringBuilder();

        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
            }
            else if (
                character == '+' &&
                builder.Length == 0)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool IsValidEmail(
        string email)
    {
        try
        {
            var address =
                new System.Net.Mail.MailAddress(
                    email);

            return string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string HtmlEncode(
        string? value)
    {
        return WebUtility.HtmlEncode(
            value ?? string.Empty);
    }

    private static string HtmlAttributeEncode(
        string? value)
    {
        return WebUtility.HtmlEncode(
            value ?? string.Empty);
    }

    private async Task<byte[]> ConvertHtmlToPdfBytesAsync(
    string html,
    Guid portfolioId,
    CancellationToken cancellationToken = default)
    {
        var totalStopwatch =
            System.Diagnostics.Stopwatch.StartNew();

        try
        {

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] EnsureBrowserAsync START");

            var stopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            var browser =
                await EnsureBrowserAsync(
                    cancellationToken);

            stopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] EnsureBrowserAsync END " +
                $"=> {stopwatch.ElapsedMilliseconds} ms " +
                $"({stopwatch.Elapsed.TotalSeconds:F2}s)");

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] NewContextAsync START");

            stopwatch.Restart();

            await using var context =
                await browser.NewContextAsync();

            stopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] NewContextAsync END " +
                $"=> {stopwatch.ElapsedMilliseconds} ms " +
                $"({stopwatch.Elapsed.TotalSeconds:F2}s)");

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] NewPageAsync START");

            stopwatch.Restart();

            await using var page =
                await context.NewPageAsync();

            stopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] NewPageAsync END " +
                $"=> {stopwatch.ElapsedMilliseconds} ms " +
                $"({stopwatch.Elapsed.TotalSeconds:F2}s)");

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] SetContentAsync START " +
                $"HTML length: {html.Length:N0} characters");

            stopwatch.Restart();

            await page.SetContentAsync(
                html,
                new PageSetContentOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

            stopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] SetContentAsync END " +
                $"=> {stopwatch.ElapsedMilliseconds} ms " +
                $"({stopwatch.Elapsed.TotalSeconds:F2}s)");

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] PdfAsync START");

            stopwatch.Restart();

            var pdfBytes =
                await page.PdfAsync(
                    new PagePdfOptions
                    {
                        Format = "A4",
                        PrintBackground = true,
                        Margin = new Margin
                        {
                            Top = "20px",
                            Bottom = "20px",
                            Left = "20px",
                            Right = "20px"
                        }
                    });

            stopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] PdfAsync END " +
                $"=> {stopwatch.ElapsedMilliseconds} ms " +
                $"({stopwatch.Elapsed.TotalSeconds:F2}s)");

            if (pdfBytes == null ||
                pdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Playwright generated an empty PDF.");
            }

            if (!IsPdf(pdfBytes))
            {
                throw new InvalidOperationException(
                    "Playwright generated invalid PDF data.");
            }

            totalStopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] TOTAL " +
                $"=> {totalStopwatch.ElapsedMilliseconds} ms " +
                $"({totalStopwatch.Elapsed.TotalSeconds:F2}s), " +
                $"PDF size: {pdfBytes.Length:N0} bytes");

            return pdfBytes;
        }
        catch (OperationCanceledException)
        {
            totalStopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] CANCELLED after " +
                $"{totalStopwatch.ElapsedMilliseconds} ms " +
                $"({totalStopwatch.Elapsed.TotalSeconds:F2}s)");

            throw;
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();

            Console.WriteLine(
                $"[PLAYWRIGHT] [{portfolioId}] FAILED after " +
                $"{totalStopwatch.ElapsedMilliseconds} ms " +
                $"({totalStopwatch.Elapsed.TotalSeconds:F2}s)");

            throw;
        }
    }

    private static async Task<IBrowser> EnsureBrowserAsync(
        CancellationToken cancellationToken)
    {
        if (_browser != null &&
            _browser.IsConnected)
        {
            return _browser;
        }

        await _browserLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_browser != null &&
                _browser.IsConnected)
            {
                return _browser;
            }

            _playwright ??=
                await Playwright.CreateAsync();

            _browser =
                await _playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                        Args = new[]
                        {
                            "--no-sandbox",
                            "--disable-dev-shm-usage"
                        }
                    });

            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public static async Task ShutdownBrowserAsync()
    {
        await _browserLock.WaitAsync();

        try
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _playwright?.Dispose();
            _playwright = null;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {

        return ValueTask.CompletedTask;
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

    private Guid GetUserIdentifier()
    {
        return
            _requestContext
                .User
                .UserIdentifier;
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

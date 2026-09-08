using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Domain.Common.ContextUser;
using TheIgKnightWorker.Domain.Entities;
using TheIgKnightWorker.Domain.UnitOfWork;

namespace TheIgKnightWorker.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IFileService _fileService;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public DocumentService(
        IFileService fileService,
        IFileStorageProvider fileStorageProvider,
        IUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _fileService = fileService;
        _fileStorageProvider = fileStorageProvider;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<(Guid DocumentId, string FileUrl)> UploadAndSaveAsync(
        IFormFile file,
        Guid tenantId,
        Guid portfolioId)
    {
        var uploadResult = await _fileService.UploadAsync(file);

        var document = new Document
        {
            DocumentId = Guid.NewGuid(),
            PortfolioId = portfolioId,
            FileName = uploadResult.FileName,
            FileUrl = uploadResult.FileUrl,
            FileType = uploadResult.FileType,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _requestContext.User.UserIdentifier
        };

        var id = await _unitOfWork.DocumentRepository.AddDocumentAsync(document);

        return (id, uploadResult.FileUrl);
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var doc = await _unitOfWork.DocumentRepository.GetDocumentByIdAsync(documentId);

        if (doc == null)
            return;

        // Remove file from storage
        await _fileService.DeleteAsync(doc.FileUrl);

        // Remove DB record
        await _unitOfWork.DocumentRepository.DeleteDocumentAsync(documentId);
    }

    public async Task<(Guid DocumentId, string FileUrl, string FileName)?> GetDocumentAsync(
        Guid documentId)
    {
        var doc = await _unitOfWork.DocumentRepository.GetDocumentByIdAsync(documentId);

        if (doc == null)
            return null;

        var secureFileUrl = _fileStorageProvider.GetSecureFileUrl(doc.FileUrl);

        return (
            doc.DocumentId,
            secureFileUrl,
            doc.FileName
        );
    }

    public async Task<(Guid DocumentId, string FileUrl, string FileName)?> GetDocumentsByPortfolioIdAsync(
        Guid portfolioId)
    {
        var doc = await _unitOfWork.DocumentRepository.GetDocumentsByPortfolioIdAsync(portfolioId);

        if (doc == null)
            return null;

        var secureFileUrl = _fileStorageProvider.GetSecureFileUrl(doc.FileUrl);

        return (
            doc.DocumentId,
            secureFileUrl,
            doc.FileName
        );
    }
}
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using IamMaturityStudio.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IamMaturityStudio.Infrastructure.Services;

public class StorageSasService : IBlobStorageService
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;

    public StorageSasService(BlobServiceClient? blobServiceClient = null, IConfiguration? configuration = null)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration?["Azure:EvidenceContainer"] ?? "evidence";
    }

    public Task<BlobUploadSasResult> GetUploadSasAsync(
        Guid orgId,
        Guid assessmentId,
        Guid questionId,
        Guid requestId,
        string fileName,
        string fileType,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (fileSizeBytes <= 0 || fileSizeBytes > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("File size exceeds the 25 MB limit.");
        }

        if (!AllowedContentTypes.Contains(fileType))
        {
            throw new InvalidOperationException($"Unsupported content type '{fileType}'.");
        }

        var safeFileName = SanitizeFileName(fileName);
        var blobName = $"evidence/{orgId}/{assessmentId}/{questionId}/{requestId}-{Guid.NewGuid():N}-{safeFileName}";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        if (_blobServiceClient is null)
        {
            var fakeUrl = $"https://example.blob.core.windows.net/{_containerName}/{Uri.EscapeDataString(blobName)}?sastoken=fake";
            return Task.FromResult(new BlobUploadSasResult(fakeUrl, blobName, expiresAt));
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var sas = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresOn = expiresAt,
            ContentType = fileType
        };
        sas.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var uploadUrl = BuildSasUrl(blobClient, sas, expiresAt);
        return Task.FromResult(new BlobUploadSasResult(uploadUrl, blobName, expiresAt));
    }

    private string BuildSasUrl(BlobClient blobClient, BlobSasBuilder sasBuilder, DateTimeOffset expiresAt)
    {
        if (blobClient.CanGenerateSasUri)
        {
            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        var delegation = _blobServiceClient!
            .GetUserDelegationKey(DateTimeOffset.UtcNow.AddMinutes(-5), expiresAt, CancellationToken.None)
            .Value;

        var token = sasBuilder.ToSasQueryParameters(delegation, _blobServiceClient.AccountName).ToString();
        return $"{blobClient.Uri}?{token}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName.Trim())
        {
            builder.Append(invalid.Contains(ch) || ch == '/' || ch == '\\' ? '-' : ch);
        }

        var cleaned = builder.ToString();
        return string.IsNullOrWhiteSpace(cleaned) ? "upload.bin" : cleaned;
    }
}
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IamMaturityStudio.Infrastructure.Reports;

public interface IReportStorage
{
    Task<StoredReportLocation> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken);
}

public sealed record StoredReportLocation(string ReportUrl, string StorageMode);

public sealed class ReportStorage : IReportStorage
{
    private readonly ReportOptions _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ReportStorage> _logger;

    public ReportStorage(IOptions<ReportOptions> options, IHostEnvironment hostEnvironment, ILogger<ReportStorage> logger)
    {
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<StoredReportLocation> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken)
    {
        if (string.Equals(_options.StorageMode, "Blob", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return await SaveToBlobAsync(content, fileName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Blob report storage failed; falling back to local storage.");
            }
        }

        return await SaveToLocalAsync(content, fileName, cancellationToken);
    }

    private async Task<StoredReportLocation> SaveToBlobAsync(byte[] content, string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BlobConnectionString))
        {
            throw new InvalidOperationException("Report:BlobConnectionString is required when StorageMode=Blob.");
        }

        var container = new BlobContainerClient(_options.BlobConnectionString, _options.BlobContainer);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(fileName);
        await using var stream = new MemoryStream(content);
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);

        return new StoredReportLocation(blob.Uri.ToString(), "Blob");
    }

    private async Task<StoredReportLocation> SaveToLocalAsync(byte[] content, string fileName, CancellationToken cancellationToken)
    {
        var root = Path.Combine(_hostEnvironment.ContentRootPath, _options.LocalFolder);
        Directory.CreateDirectory(root);

        var filePath = Path.Combine(root, fileName);
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        return new StoredReportLocation(new Uri(filePath).AbsoluteUri, "Local");
    }
}

using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Infrastructure.Services;

public class StorageSasService : IStorageSasService
{
    public string CreateUploadUrl(string blobName, TimeSpan lifetime)
    {
        var expires = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        return $"https://example.blob.core.windows.net/evidence/{Uri.EscapeDataString(blobName)}?sastoken=fake&exp={expires}";
    }
}
using Chronolog.Server.Core;

namespace Chronolog.Server.Tests;

internal sealed class TestImageUploader : IJournalImageUploader
{
    public string? RequestedDeviceId { get; private set; }
    public string? RequestedContentType { get; private set; }
    public List<string> DeletedImageKeys { get; } = new();

    public Task<ImageUpload> CreateUploadAsync(string deviceId, string recordId, string contentType)
    {
        RequestedDeviceId = deviceId;
        RequestedContentType = contentType;
        var extension = contentType == "image/png" ? "png" : "jpg";
        return Task.FromResult(new ImageUpload($"images/{deviceId}/{recordId}.{extension}", "https://example.com/upload"));
    }

    public Task DeleteAsync(string imageKey)
    {
        DeletedImageKeys.Add(imageKey);
        return Task.CompletedTask;
    }

    public Task<ImageDownload> CreateDownloadAsync(string imageKey)
    {
        return Task.FromResult(new ImageDownload("https://example.com/download"));
    }
}

namespace Chronolog.Server.Core;

public interface IJournalImageUploader
{
    Task<ImageUpload> CreateUploadAsync(string deviceId, string recordId, string contentType);
    Task<ImageDownload> CreateDownloadAsync(string imageKey);
    Task DeleteAsync(string imageKey);
}
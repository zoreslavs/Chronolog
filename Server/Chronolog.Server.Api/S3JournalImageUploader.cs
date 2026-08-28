using Chronolog.Server.Core;
using Amazon.S3.Model;
using Amazon.S3;

namespace Chronolog.Server.Api;

public sealed class S3JournalImageUploader : IJournalImageUploader
{
    private readonly IAmazonS3 s3;
    private readonly string bucketName;

    public S3JournalImageUploader(IAmazonS3 s3, string bucketName)
    {
        this.s3 = s3;
        this.bucketName = bucketName;
    }

    public Task<ImageUpload> CreateUploadAsync(string deviceId, string recordId, string contentType)
    {
        var imageKey = $"images/{deviceId}/{recordId}.{GetFileExtension(contentType)}";
        var uploadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = imageKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        return Task.FromResult(new ImageUpload(imageKey, uploadUrl));
    }

    public async Task DeleteAsync(string imageKey)
    {
        await s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = imageKey
        });
    }

    public Task<ImageDownload> CreateDownloadAsync(string imageKey)
    {
        var downloadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = imageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        return Task.FromResult(new ImageDownload(downloadUrl));
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType == "image/png" ? "png" : "jpg";
    }
}
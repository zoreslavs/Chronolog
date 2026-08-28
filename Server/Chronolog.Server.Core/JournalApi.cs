using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Chronolog.Server.Core;

public sealed class JournalApi
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IJournalRecordStore recordStore;
    private readonly IJournalImageUploader imageUploader;
    private readonly Action<Exception>? errorLogger;

    public JournalApi(IJournalRecordStore recordStore, IJournalImageUploader imageUploader, Action<Exception>? errorLogger = null)
    {
        this.recordStore = recordStore;
        this.imageUploader = imageUploader;
        this.errorLogger = errorLogger;
    }

    public async Task<JournalApiResponse> HandleAsync(JournalApiRequest request)
    {
        try
        {
            return await HandleRequestAsync(request);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            errorLogger?.Invoke(exception);
            return JsonResponse(500, new { message = "An unexpected server error occurred." });
        }
    }

    private async Task<JournalApiResponse> HandleRequestAsync(JournalApiRequest request)
    {
        if (!TryGetValidDeviceId(request.DeviceId, out var deviceId))
            return JsonResponse(400, new { message = "Invalid device ID." });

        if (request.RouteKey == "GET /export.csv")
        {
            var records = await recordStore.ListAsync(deviceId);

            return new JournalApiResponse(
                200,
                new Dictionary<string, string> { ["content-type"] = "text/csv; charset=utf-8" },
                RecordsCsv.Create(records));
        }

        if (request.RouteKey == "GET /records")
        {
            var records = await recordStore.ListAsync(deviceId);

            return JsonResponse(200, records);
        }

        if (request.RouteKey == "POST /uploads")
        {
            if (!TryDeserialize(request.Body, out ImageUploadRequest? uploadRequest)
                || uploadRequest is null
                || !TryGetValidId(uploadRequest.RecordId, out var recordId)
                || !TryGetSupportedImageContentType(uploadRequest.ContentType, out var contentType))
                return BadRequest();

            var upload = await imageUploader.CreateUploadAsync(deviceId, recordId, contentType);

            return JsonResponse(200, upload);
        }

        if (request.RouteKey == "GET /records/{id}/image")
        {
            if (!TryGetValidId(request.RecordId, out var recordId))
                return BadRequest();

            var existingRecord = await recordStore.GetAsync(deviceId, recordId);
            if (existingRecord == null)
                return JsonResponse(404, new { message = "Record not found." });

            var download = await imageUploader.CreateDownloadAsync(existingRecord.ImageKey);
            return JsonResponse(200, download);
        }

        if (request.RouteKey == "DELETE /records/{id}")
        {
            if (!TryGetValidId(request.RecordId, out var recordId))
                return BadRequest();

            var existingRecord = await recordStore.GetAsync(deviceId, recordId);
            if (existingRecord != null)
            {
                await recordStore.DeleteAsync(deviceId, recordId);
                await imageUploader.DeleteAsync(existingRecord.ImageKey);
            }

            return new JournalApiResponse(204, new Dictionary<string, string>(), string.Empty);
        }

        if (request.RouteKey is not ("POST /records" or "PUT /records/{id}"))
            return JsonResponse(404, new { message = "Route not found." });

        if (!TryDeserialize(request.Body, out JournalRecordRequest? recordRequest)
            || !TryCreateRecord(deviceId, recordRequest, out var record))
            return BadRequest();

        if (request.RouteKey == "PUT /records/{id}")
        {
            if (!TryGetValidId(request.RecordId, out var recordId) || recordId != record.Id)
                return BadRequest();

            var existingRecord = await recordStore.GetAsync(deviceId, recordId);
            if (existingRecord == null)
                return JsonResponse(404, new { message = "Record not found." });

            if (record.ImageKey != existingRecord.ImageKey)
                await imageUploader.DeleteAsync(existingRecord.ImageKey);

            record = record with { CreatedAtUtc = existingRecord.CreatedAtUtc };
        }

        await recordStore.SaveAsync(record);

        return JsonResponse(request.RouteKey == "POST /records" ? 201 : 200, record);
    }

    private static JournalApiResponse JsonResponse(int statusCode, object body)
    {
        return new JournalApiResponse(
            statusCode,
            new Dictionary<string, string> { ["content-type"] = "application/json" },
            JsonSerializer.Serialize(body, JsonOptions));
    }

    private static JournalApiResponse BadRequest()
    {
        return JsonResponse(400, new { message = "Invalid request body." });
    }

    private static bool TryDeserialize<T>(string? body, out T? request)
    {
        try
        {
            request = JsonSerializer.Deserialize<T>(body ?? string.Empty, JsonOptions);
            return request is not null;
        }
        catch (JsonException)
        {
            request = default;
            return false;
        }
    }

    private static bool TryCreateRecord(string deviceId, JournalRecordRequest? request, [NotNullWhen(true)] out RemoteJournalRecord? record)
    {
        record = null;

        if (request is null
            || !TryGetValidId(request.Id, out var id)
            || !DateTimeOffset.TryParse(request.CreatedAtUtc, out _)
            || !DateTimeOffset.TryParse(request.UpdatedAtUtc, out _)
            || string.IsNullOrWhiteSpace(request.Content)
            || string.IsNullOrWhiteSpace(request.ImageSource))
            return false;

        if (!TryGetValidImageKey(deviceId, id, request.ImageKey, out var imageKey))
            return false;

        record = new RemoteJournalRecord(
            deviceId,
            id,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.Content,
            request.ImageSource,
            imageKey,
            request.IsHighlighted);
        return true;
    }

    private static bool TryGetValidId(string? value, out string id)
    {
        id = value ?? string.Empty;
        return Guid.TryParse(id, out _);
    }

    private static bool TryGetValidDeviceId(string? value, out string deviceId)
    {
        deviceId = value ?? string.Empty;
        return deviceId.Length is > 0 and <= 128
               && deviceId.All(character => char.IsLetterOrDigit(character) || character == '-');
    }

    private static bool TryGetSupportedImageContentType(string? value, out string contentType)
    {
        contentType = value ?? string.Empty;
        return contentType is "image/jpeg" or "image/png";
    }

    private static bool TryGetValidImageKey(string deviceId, string recordId, string? value, out string imageKey)
    {
        imageKey = value ?? string.Empty;
        return imageKey == $"images/{deviceId}/{recordId}.jpg" || imageKey == $"images/{deviceId}/{recordId}.png";
    }

    private sealed record JournalRecordRequest(
        string? Id,
        string? CreatedAtUtc,
        string? UpdatedAtUtc,
        string? Content,
        string? ImageSource,
        string? ImageKey,
        bool IsHighlighted);

    private sealed record ImageUploadRequest(string? RecordId, string? ContentType);
}
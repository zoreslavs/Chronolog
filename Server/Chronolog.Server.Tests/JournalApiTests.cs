using Chronolog.Server.Core;
using System.Text.Json;
using Xunit;

namespace Chronolog.Server.Tests;

public sealed class JournalApiTests
{
    private const string DeviceId = "android-a1b2c3d4e5f60708";
    private const string SecondDeviceId = "android-1020304050607080";
    private const string RecordId = "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9";
    private const string CreatedAtUtc = "2026-08-27T09:30:00.000Z";

    [Fact]
    public async Task HandleAsync_ReturnsBadRequestWithoutDeviceId()
    {
        var api = new JournalApi(new InMemoryJournalRecordStore(), new TestImageUploader());

        var response = await api.HandleAsync(new JournalApiRequest("GET /records", null));

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid device ID.", response.Body);
    }

    [Fact]
    public async Task HandleAsync_StoresAndReturnsHighlightedRecord()
    {
        var store = new InMemoryJournalRecordStore();
        var api = new JournalApi(store, new TestImageUploader());

        var response = await api.HandleAsync(new JournalApiRequest(
            "POST /records",
            """
            {
              "id": "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
              "createdAtUtc": "2026-08-27T09:30:00.000Z",
              "updatedAtUtc": "2026-08-27T09:30:00.000Z",
              "content": "Morning walk",
              "imageSource": "Gallery",
              "imageKey": "images/android-a1b2c3d4e5f60708/2799dc7a-8cdc-4127-8a90-6e67b6abe7d9.jpg",
              "isHighlighted": true
            }
            """,
            DeviceId));

        using var body = JsonDocument.Parse(response.Body);

        Assert.Equal(201, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("isHighlighted").GetBoolean());
        Assert.Single(store.Records);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequestForImageKeyOutsideTheDeviceRecordPrefix()
    {
        var api = new JournalApi(new InMemoryJournalRecordStore(), new TestImageUploader());

        var response = await api.HandleAsync(new JournalApiRequest(
            "POST /records",
            """
            {
              "id": "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
              "createdAtUtc": "2026-08-27T09:30:00.000Z",
              "updatedAtUtc": "2026-08-27T09:30:00.000Z",
              "content": "Morning walk",
              "imageSource": "Gallery",
              "imageKey": "images/android-1020304050607080/other-record.jpg"
            }
            """,
            DeviceId));

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid request body.", response.Body);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyRecordsForTheRequestDevice()
    {
        var store = new InMemoryJournalRecordStore();
        await store.SaveAsync(CreateRecord(DeviceId, RecordId, "First device record"));
        await store.SaveAsync(CreateRecord(SecondDeviceId, "8b7c95f8-4016-41a4-afec-5e450be6231d", "Second device record"));
        var api = new JournalApi(store, new TestImageUploader());

        var response = await api.HandleAsync(new JournalApiRequest("GET /records", null, DeviceId));

        using var body = JsonDocument.Parse(response.Body);
        Assert.Equal(200, response.StatusCode);
        Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal(RecordId, body.RootElement[0].GetProperty("id").GetString());
    }

    [Fact]
        public async Task HandleAsync_UpdatesAnExistingRecordAndDeletesItsReplacedImage()
    {
        var store = new InMemoryJournalRecordStore();
        await store.SaveAsync(CreateRecord(DeviceId, RecordId, "Original entry", "images/android-a1b2c3d4e5f60708/old.jpg"));
        var imageUploader = new TestImageUploader();
        var api = new JournalApi(store, imageUploader);

        var response = await api.HandleAsync(new JournalApiRequest(
            "PUT /records/{id}",
            """
            {
              "id": "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
              "createdAtUtc": "2026-08-27T12:00:00.000Z",
              "updatedAtUtc": "2026-08-27T10:30:00.000Z",
              "content": "Updated entry",
              "imageSource": "Gallery",
              "imageKey": "images/android-a1b2c3d4e5f60708/2799dc7a-8cdc-4127-8a90-6e67b6abe7d9.png"
            }
            """,
            DeviceId,
            RecordId));

        var updatedRecord = await store.GetAsync(DeviceId, RecordId);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Updated entry", updatedRecord!.Content);
        Assert.Equal(CreatedAtUtc, updatedRecord.CreatedAtUtc);
        Assert.Equal("2026-08-27T10:30:00.000Z", updatedRecord.UpdatedAtUtc);
            Assert.Equal("images/android-a1b2c3d4e5f60708/old.jpg", Assert.Single(imageUploader.DeletedImageKeys));
        }

        [Fact]
        public async Task HandleAsync_CreatesAnImageDownloadUrlOnlyForTheRequestDeviceRecord()
        {
            var store = new InMemoryJournalRecordStore();
            await store.SaveAsync(CreateRecord(DeviceId, RecordId, "First device record"));
            var api = new JournalApi(store, new TestImageUploader());

            var response = await api.HandleAsync(new JournalApiRequest("GET /records/{id}/image", null, DeviceId, RecordId));

            using var body = JsonDocument.Parse(response.Body);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("https://example.com/download", body.RootElement.GetProperty("downloadUrl").GetString());
        }

    [Fact]
    public async Task HandleAsync_DeletesOnlyTheRequestedDeviceRecordAndIsIdempotent()
    {
        var store = new InMemoryJournalRecordStore();
        await store.SaveAsync(CreateRecord(DeviceId, RecordId, "First device record"));
        await store.SaveAsync(CreateRecord(SecondDeviceId, RecordId, "Second device record"));
        var imageUploader = new TestImageUploader();
        var api = new JournalApi(store, imageUploader);

        var firstResponse = await api.HandleAsync(new JournalApiRequest("DELETE /records/{id}", null, DeviceId, RecordId));
        var retryResponse = await api.HandleAsync(new JournalApiRequest("DELETE /records/{id}", null, DeviceId, RecordId));

        Assert.Equal(204, firstResponse.StatusCode);
        Assert.Equal(204, retryResponse.StatusCode);
        Assert.Null(await store.GetAsync(DeviceId, RecordId));
        Assert.NotNull(await store.GetAsync(SecondDeviceId, RecordId));
        Assert.Single(imageUploader.DeletedImageKeys);
    }

    [Fact]
    public async Task HandleAsync_ExportsStoredRecordsAsCsv()
    {
        var store = new InMemoryJournalRecordStore();
        await store.SaveAsync(new RemoteJournalRecord(
            DeviceId,
            "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
            "2026-08-27T09:30:00.000Z",
            "2026-08-27T09:30:00.000Z",
            "Morning walk",
            "Gallery",
            "images/2799dc7a.jpg",
            false));
        var api = new JournalApi(store, new TestImageUploader());

        var response = await api.HandleAsync(new JournalApiRequest("GET /export.csv", null, DeviceId));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("text/csv; charset=utf-8", response.Headers["content-type"]);
        Assert.Contains("Morning walk", response.Body);
    }

    [Fact]
    public async Task HandleAsync_ReturnsStoredRecordsWithServerMetadata()
    {
        var store = new InMemoryJournalRecordStore();
        await store.SaveAsync(new RemoteJournalRecord(
            DeviceId,
            "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
            "2026-08-27T09:30:00.000Z",
            "2026-08-27T09:30:00.000Z",
            "Morning walk #highlight",
            "Gallery",
            "images/2799dc7a.jpg",
            true));
        var api = new JournalApi(store, new TestImageUploader());

        var response = await api.HandleAsync(new JournalApiRequest("GET /records", null, DeviceId));

        using var body = JsonDocument.Parse(response.Body);

        Assert.Equal(200, response.StatusCode);
        Assert.True(body.RootElement[0].GetProperty("isHighlighted").GetBoolean());
    }

    [Fact]
    public async Task HandleAsync_CreatesImageUploadUrlForTheRequestedImageType()
    {
        var imageUploader = new TestImageUploader();
        var api = new JournalApi(new InMemoryJournalRecordStore(), imageUploader);

        var response = await api.HandleAsync(new JournalApiRequest(
            "POST /uploads",
            """
            {
              "recordId": "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
              "contentType": "image/png"
            }
            """,
            DeviceId));

        using var body = JsonDocument.Parse(response.Body);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("image/png", imageUploader.RequestedContentType);
        Assert.Equal($"images/{DeviceId}/2799dc7a-8cdc-4127-8a90-6e67b6abe7d9.png", body.RootElement.GetProperty("imageKey").GetString());
        Assert.Equal("https://example.com/upload", body.RootElement.GetProperty("uploadUrl").GetString());
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequestForMalformedRecordJson()
    {
        var api = new JournalApi(new InMemoryJournalRecordStore(), new TestImageUploader());
        var response = await api.HandleAsync(new JournalApiRequest(
            "POST /records",
            "{ not valid json",
            DeviceId));

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid request body.", response.Body);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequestForRecordWithMissingRequiredFields()
    {
        var api = new JournalApi(new InMemoryJournalRecordStore(), new TestImageUploader());
        var response = await api.HandleAsync(new JournalApiRequest(
            "POST /records",
            """
            {
              "id": "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
              "createdAtUtc": "2026-08-27T09:30:00.000Z",
              "updatedAtUtc": "2026-08-27T09:30:00.000Z",
              "imageSource": "Gallery",
              "imageKey": "images/2799dc7a.jpg"
            }
            """,
            DeviceId));

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid request body.", response.Body);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericServerErrorWhenStoreFails()
    {
        var api = new JournalApi(new FailingJournalRecordStore(), new TestImageUploader());
        var response = await api.HandleAsync(new JournalApiRequest("GET /records", null, DeviceId));

        Assert.Equal(500, response.StatusCode);
        Assert.Contains("An unexpected server error occurred.", response.Body);
        Assert.DoesNotContain("DynamoDB is unavailable.", response.Body);
    }

    private sealed class InMemoryJournalRecordStore : IJournalRecordStore
    {
        public List<RemoteJournalRecord> Records { get; } = new();

        public Task SaveAsync(RemoteJournalRecord record)
        {
            Records.RemoveAll(existingRecord => existingRecord.DeviceId == record.DeviceId && existingRecord.Id == record.Id);
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<RemoteJournalRecord?> GetAsync(string deviceId, string recordId)
        {
            return Task.FromResult(Records.SingleOrDefault(record => record.DeviceId == deviceId && record.Id == recordId));
        }

        public Task<IReadOnlyList<RemoteJournalRecord>> ListAsync(string deviceId)
        {
            return Task.FromResult<IReadOnlyList<RemoteJournalRecord>>(Records.Where(record => record.DeviceId == deviceId).ToList());
        }

        public Task DeleteAsync(string deviceId, string recordId)
        {
            Records.RemoveAll(record => record.DeviceId == deviceId && record.Id == recordId);
            return Task.CompletedTask;
        }
    }

    private static RemoteJournalRecord CreateRecord(string deviceId, string recordId, string content, string? imageKey = null)
    {
        return new RemoteJournalRecord(
            deviceId,
            recordId,
            CreatedAtUtc,
            CreatedAtUtc,
            content,
            "Gallery",
            imageKey ?? $"images/{deviceId}/{recordId}.jpg",
            false);
    }

    private sealed class FailingJournalRecordStore : IJournalRecordStore
    {
        public Task SaveAsync(RemoteJournalRecord record)
        {
            return Task.FromException(new InvalidOperationException("DynamoDB is unavailable."));
        }

        public Task<RemoteJournalRecord?> GetAsync(string deviceId, string recordId)
        {
            return Task.FromException<RemoteJournalRecord?>(new InvalidOperationException("DynamoDB is unavailable."));
        }

        public Task<IReadOnlyList<RemoteJournalRecord>> ListAsync(string deviceId)
        {
            return Task.FromException<IReadOnlyList<RemoteJournalRecord>>(new InvalidOperationException("DynamoDB is unavailable."));
        }

        public Task DeleteAsync(string deviceId, string recordId)
        {
            return Task.FromException(new InvalidOperationException("DynamoDB is unavailable."));
        }
    }
}

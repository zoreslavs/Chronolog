using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using Chronolog.Persistence;
using Chronolog.Domain;
using UnityEngine;
using System.Text;
using System.IO;
using System;

namespace Chronolog.Presentation
{
    public enum JournalSyncStatus { Offline, Syncing, Synced, Failed }

    public sealed class JournalSyncService : MonoBehaviour
    {
        [SerializeField] private JournalNetworkMonitor networkMonitor;
        private readonly HashSet<Guid> syncingRecordIds = new();
        private string deviceId;
        private JournalImageStorage imageStorage;
        private IJournalRecordRepository repository;
        private bool hadFailures;

        public JournalSyncStatus Status { get; private set; }
        public event Action SyncCompleted;
        public event Action<JournalSyncStatus> StatusChanged;

        private void Awake()
        {
            deviceId = JournalDeviceId.Get();
            imageStorage = new JournalImageStorage(Application.persistentDataPath);
            repository = new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath());
        }

        private void Start()
        {
            networkMonitor.BecameReachable += OnNetworkBecameReachable;
            networkMonitor.BecameUnreachable += OnNetworkBecameUnreachable;

            SetStatus(JournalSyncStatus.Syncing);
            StartCoroutine(SyncPendingRecords());
        }

        private void OnDestroy()
        {
            networkMonitor.BecameReachable -= OnNetworkBecameReachable;
            networkMonitor.BecameUnreachable -= OnNetworkBecameUnreachable;
        }

        public void Sync(JournalRecord record)
        {
            if (record == null || syncingRecordIds.Contains(record.Id))
                return;

            SetStatus(JournalSyncStatus.Syncing);
            StartCoroutine(SyncRecord(record));
        }

        private IEnumerator SyncPendingRecords()
        {
            if (networkMonitor != null && !networkMonitor.IsReachable)
            {
                SetStatus(JournalSyncStatus.Offline);
                yield break;
            }

            hadFailures = false;

            foreach (var record in repository.GetAll())
            {
                if (record.IsDeleted || record.SyncState != JournalSyncState.Synced)
                    yield return SyncRecord(record);
            }

            yield return ReloadRemoteMetadata();

            if (syncingRecordIds.Count == 0 && Status != JournalSyncStatus.Offline)
                SetStatus(hadFailures ? JournalSyncStatus.Failed : JournalSyncStatus.Synced);
        }

        private IEnumerator SyncRecord(JournalRecord record)
        {
            if (!syncingRecordIds.Add(record.Id))
                yield break;

            SetStatus(JournalSyncStatus.Syncing);
            record.MarkSyncing();
            repository.Save(record);

            if (record.IsDeleted)
            {
                string deleteErrorMessage = null;
                yield return DeleteRemoteRecord(record.Id, error => deleteErrorMessage = error);
                if (deleteErrorMessage != null)
                {
                    MarkFailed(record, deleteErrorMessage);
                    CompleteSync(record);
                    yield break;
                }

                DeleteLocalRecord(record);
                CompleteSync(record);
                SyncCompleted?.Invoke();
                yield break;
            }

            if (!File.Exists(record.LocalImagePath))
            {
                MarkFailed(record, "The local image file is missing.");
                CompleteSync(record);
                yield break;
            }

            string errorMessage = null;
            var contentType = JournalImageContentType.GetForFilePath(record.LocalImagePath);
            var upload = default(ImageUploadResponse);
            yield return RequestUploadUrl(record.Id, contentType, result => upload = result, error => errorMessage = error);
            if (errorMessage != null)
            {
                MarkFailed(record, errorMessage);
                CompleteSync(record);
                yield break;
            }

            yield return UploadImage(upload.uploadUrl, record.LocalImagePath, contentType, error => errorMessage = error);
            if (errorMessage != null)
            {
                MarkFailed(record, errorMessage);
                CompleteSync(record);
                yield break;
            }

            var remoteRecord = default(RemoteRecordResponse);
            yield return SaveRemoteRecord(record, upload.imageKey, result => remoteRecord = result, error => errorMessage = error);
            if (errorMessage != null)
            {
                MarkFailed(record, errorMessage);
                CompleteSync(record);
                yield break;
            }

            record.MarkSynced(remoteRecord.imageKey, remoteRecord.isHighlighted, record.UpdatedAtUtc);
            repository.Save(record);
            CompleteSync(record);
            SyncCompleted?.Invoke();
        }

        private IEnumerator RequestUploadUrl(Guid recordId, string contentType, Action<ImageUploadResponse> complete, Action<string> failed)
        {
            var body = JsonUtility.ToJson(new ImageUploadRequest { recordId = recordId.ToString("D"), contentType = contentType });
            using var request = CreateJsonRequest("uploads", "POST", body);
            yield return request.SendWebRequest();
            if (!TryGetError(request, out var errorMessage))
            {
                failed(errorMessage);
                yield break;
            }
            complete(JsonUtility.FromJson<ImageUploadResponse>(request.downloadHandler.text));
        }

        private IEnumerator UploadImage(string uploadUrl, string localImagePath, string contentType, Action<string> failed)
        {
            using var request = UnityWebRequest.Put(uploadUrl, File.ReadAllBytes(localImagePath));
            request.SetRequestHeader("Content-Type", contentType);
            yield return request.SendWebRequest();
            if (!TryGetError(request, out var errorMessage))
                failed(errorMessage);
        }

        private IEnumerator SaveRemoteRecord(JournalRecord record, string imageKey, Action<RemoteRecordResponse> complete, Action<string> failed)
        {
            var body = JsonUtility.ToJson(new SaveRecordRequest { id = record.Id.ToString("D"), createdAtUtc = record.CreatedAtUtc.ToString("O"), updatedAtUtc = record.UpdatedAtUtc.ToString("O"), content = record.Content, imageSource = record.ImageSource.ToString(), imageKey = imageKey, isHighlighted = record.IsHighlighted });
            var isExistingRecord = !string.IsNullOrWhiteSpace(record.RemoteImageKey);
            using var request = CreateJsonRequest(isExistingRecord ? $"records/{record.Id:D}" : "records", isExistingRecord ? "PUT" : "POST", body);
            yield return request.SendWebRequest();
            if (!TryGetError(request, out var errorMessage))
            {
                failed(errorMessage);
                yield break;
            }
            complete(JsonUtility.FromJson<RemoteRecordResponse>(request.downloadHandler.text));
        }

        private IEnumerator DeleteRemoteRecord(Guid recordId, Action<string> failed)
        {
            using var request = CreateJsonRequest($"records/{recordId:D}", "DELETE", null);
            yield return request.SendWebRequest();
            if (!TryGetError(request, out var errorMessage))
                failed(errorMessage);
        }

        private IEnumerator ReloadRemoteMetadata()
        {
            using var request = CreateRequest("records", "GET");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                if (request.result == UnityWebRequest.Result.ConnectionError)
                    SetStatus(JournalSyncStatus.Offline);
                else
                {
                    hadFailures = true;
                    SetStatus(JournalSyncStatus.Failed);
                }
                yield break;
            }

            var remoteRecords = JsonUtility.FromJson<RemoteRecordsResponse>($"{{\"records\":{request.downloadHandler.text}}}");
            if (remoteRecords?.records == null)
                yield break;

            var localRecords = new Dictionary<Guid, JournalRecord>();
            foreach (var localRecord in repository.GetAll())
            {
                localRecords[localRecord.Id] = localRecord;
            }

            foreach (var remoteRecord in remoteRecords.records)
            {
                if (Guid.TryParse(remoteRecord.id, out var recordId) && localRecords.TryGetValue(recordId, out var localRecord))
                {
                    if (localRecord.IsDeleted)
                        continue;

                    var updatedAtUtc = DateTimeOffset.TryParse(remoteRecord.updatedAtUtc, out var remoteUpdatedAtUtc)
                        ? remoteUpdatedAtUtc
                        : localRecord.UpdatedAtUtc;
                    localRecord.MarkSynced(remoteRecord.imageKey, remoteRecord.isHighlighted, updatedAtUtc);
                    repository.Save(localRecord);
                }
                else
                {
                    yield return RestoreRemoteRecord(remoteRecord);
                }
            }

            SyncCompleted?.Invoke();
        }

        private IEnumerator RestoreRemoteRecord(RemoteRecordResponse remoteRecord)
        {
            if (!Guid.TryParse(remoteRecord.id, out var recordId)
                || !DateTimeOffset.TryParse(remoteRecord.createdAtUtc, out var createdAtUtc)
                || !DateTimeOffset.TryParse(remoteRecord.updatedAtUtc, out var updatedAtUtc)
                || !Enum.TryParse(remoteRecord.imageSource, out JournalImageSource imageSource)
                || string.IsNullOrWhiteSpace(remoteRecord.content)
                || string.IsNullOrWhiteSpace(remoteRecord.imageKey))
            {
                hadFailures = true;
                Debug.LogWarning("A remote journal record has an unsupported format.");
                yield break;
            }

            var download = default(ImageDownloadResponse);
            var errorMessage = default(string);
            yield return RequestImageDownload(recordId, result => download = result, error => errorMessage = error);
            if (errorMessage != null || string.IsNullOrWhiteSpace(download?.downloadUrl))
            {
                hadFailures = true;
                Debug.LogWarning($"Journal record {recordId} could not be restored: {errorMessage ?? "The image URL is missing."}");
                yield break;
            }

            byte[] imageBytes = null;
            yield return DownloadImage(download.downloadUrl, result => imageBytes = result, error => errorMessage = error);
            if (errorMessage != null || imageBytes == null)
            {
                hadFailures = true;
                Debug.LogWarning($"Journal record {recordId} could not be restored: {errorMessage ?? "The image download is empty."}");
                yield break;
            }

            var fileExtension = Path.GetExtension(remoteRecord.imageKey);
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                hadFailures = true;
                Debug.LogWarning($"Journal record {recordId} could not be restored: The image key has no extension.");
                yield break;
            }

            var localImagePath = imageStorage.SaveToLocalStorage(imageBytes, fileExtension);
            var localRecord = JournalRecord.Restore(
                recordId,
                remoteRecord.content,
                imageSource,
                localImagePath,
                remoteRecord.imageKey,
                createdAtUtc,
                updatedAtUtc,
                DateTimeOffset.UtcNow,
                JournalSyncState.Synced,
                null,
                remoteRecord.isHighlighted);
            repository.Save(localRecord);
        }

        private IEnumerator RequestImageDownload(Guid recordId, Action<ImageDownloadResponse> complete, Action<string> failed)
        {
            using var request = CreateRequest($"records/{recordId:D}/image", "GET");
            yield return request.SendWebRequest();
            if (!TryGetError(request, out var errorMessage))
            {
                failed(errorMessage);
                yield break;
            }

            complete(JsonUtility.FromJson<ImageDownloadResponse>(request.downloadHandler.text));
        }

        private IEnumerator DownloadImage(string downloadUrl, Action<byte[]> complete, Action<string> failed)
        {
            using var request = UnityWebRequest.Get(downloadUrl);
            yield return request.SendWebRequest();
            if (!TryGetError(request, out var errorMessage))
            {
                failed(errorMessage);
                yield break;
            }

            complete(request.downloadHandler.data);
        }

        private UnityWebRequest CreateJsonRequest(string path, string method, string body)
        {
            var request = CreateRequest(path, method);
            if (body == null)
                return request;

            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private UnityWebRequest CreateRequest(string path, string method)
        {
            var request = new UnityWebRequest(GetApiUrl(path), method) { downloadHandler = new DownloadHandlerBuffer() };
            request.SetRequestHeader(JournalApiConfig.DeviceIdHeader, deviceId);
            return request;
        }

        private void OnNetworkBecameReachable()
        {
            if (Status is JournalSyncStatus.Offline or JournalSyncStatus.Failed)
            {
                SetStatus(JournalSyncStatus.Syncing);
                StartCoroutine(SyncPendingRecords());
            }
        }

        private void OnNetworkBecameUnreachable()
        {
            SetStatus(JournalSyncStatus.Offline);
        }

        private static string GetApiUrl(string path) => $"{JournalApiConfig.BaseUrl.TrimEnd('/')}/{path}";

        private void MarkFailed(JournalRecord record, string errorMessage)
        {
            record.MarkFailed(errorMessage);
            repository.Save(record);
            hadFailures = true;
            Debug.LogWarning($"Journal record {record.Id} could not be synced: {errorMessage}");
        }

        private void DeleteLocalRecord(JournalRecord record)
        {
            if (File.Exists(record.LocalImagePath))
                File.Delete(record.LocalImagePath);

            repository.Delete(record.Id);
        }

        private void SetStatus(JournalSyncStatus status)
        {
            if (Status == status)
                return;

            Status = status;
            StatusChanged?.Invoke(status);
        }

        private void UpdateStatusAfterSync()
        {
            if (syncingRecordIds.Count > 0 || Status == JournalSyncStatus.Offline)
                return;

            SetStatus(hadFailures ? JournalSyncStatus.Failed : JournalSyncStatus.Synced);
        }

        private void CompleteSync(JournalRecord record)
        {
            syncingRecordIds.Remove(record.Id);
            UpdateStatusAfterSync();
        }

        private bool TryGetError(UnityWebRequest request, out string errorMessage)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = $"Request failed: {request.responseCode} {request.error}";
            if (request.result == UnityWebRequest.Result.ConnectionError)
                SetStatus(JournalSyncStatus.Offline);
            return false;
        }

        [Serializable] private sealed class ImageUploadRequest { public string recordId; public string contentType; }
        [Serializable] private sealed class ImageUploadResponse { public string imageKey; public string uploadUrl; }
        [Serializable] private sealed class SaveRecordRequest { public string id; public string createdAtUtc; public string updatedAtUtc; public string content; public string imageSource; public string imageKey; public bool isHighlighted; }
        [Serializable] private sealed class ImageDownloadResponse { public string downloadUrl; }
        [Serializable] private sealed class RemoteRecordResponse { public string id; public string createdAtUtc; public string updatedAtUtc; public string content; public string imageSource; public string imageKey; public bool isHighlighted; }
        [Serializable] private sealed class RemoteRecordsResponse { public RemoteRecordResponse[] records; }
    }
}

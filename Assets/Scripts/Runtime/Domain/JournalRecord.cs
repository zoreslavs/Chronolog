using System;

namespace Chronolog.Domain
{
    public sealed class JournalRecord
    {
        public Guid Id { get; }

        public string Content { get; private set; }

        public JournalImageSource ImageSource { get; private set; }

        public string LocalImagePath { get; private set; }

        public string RemoteImageKey { get; private set; }

        public bool IsHighlighted { get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public DateTimeOffset? ServerReceivedAtUtc { get; private set; }

        public JournalSyncState SyncState { get; private set; }

        public string LastSyncError { get; private set; }

        private JournalRecord(Guid id, string content, JournalImageSource imageSource, string localImagePath, DateTimeOffset createdAtUtc, bool isHighlighted)
        {
            Id = id;
            Content = content.Trim();
            ImageSource = imageSource;
            LocalImagePath = localImagePath;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = createdAtUtc;
            IsHighlighted = isHighlighted;
            SyncState = JournalSyncState.Pending;
        }

        public static JournalRecord Create(Guid id, string content, JournalImageSource imageSource, string localImagePath, DateTimeOffset createdAtUtc, bool isHighlighted = false)
        {
            if (!JournalRecordValidator.TryValidate(content, localImagePath, out var errorMessage))
                throw new ArgumentException(errorMessage);

            return new JournalRecord(id, content, imageSource, localImagePath, createdAtUtc, isHighlighted);
        }

        public static JournalRecord Restore(
            Guid id,
            string content,
            JournalImageSource imageSource,
            string localImagePath,
            string remoteImageKey,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc,
            DateTimeOffset? serverReceivedAtUtc,
            JournalSyncState syncState,
            string lastSyncError,
            bool isHighlighted = false,
            bool isDeleted = false)
        {
            if (!JournalRecordValidator.TryValidate(content, localImagePath, out var errorMessage))
                throw new ArgumentException(errorMessage);

            var record = new JournalRecord(id, content, imageSource, localImagePath, createdAtUtc, isHighlighted)
            {
                RemoteImageKey = remoteImageKey,
                UpdatedAtUtc = updatedAtUtc,
                ServerReceivedAtUtc = serverReceivedAtUtc,
                SyncState = syncState,
                LastSyncError = lastSyncError,
                IsDeleted = isDeleted
            };

            return record;
        }

        public void MarkSyncing(DateTimeOffset updatedAtUtc)
        {
            UpdatedAtUtc = updatedAtUtc;
            SyncState = JournalSyncState.Syncing;
            LastSyncError = null;
        }

        public void MarkSynced(string remoteImageKey, bool isHighlighted, DateTimeOffset updatedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(remoteImageKey))
                throw new ArgumentException("Remote image key is required.", nameof(remoteImageKey));

            RemoteImageKey = remoteImageKey;
            IsHighlighted = isHighlighted;
            UpdatedAtUtc = updatedAtUtc;
            SyncState = JournalSyncState.Synced;
            LastSyncError = null;
        }

        public void MarkFailed(string errorMessage, DateTimeOffset updatedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Sync error message is required.", nameof(errorMessage));

            UpdatedAtUtc = updatedAtUtc;
            SyncState = JournalSyncState.Failed;
            LastSyncError = errorMessage;
        }

        public void Update(string content, JournalImageSource imageSource, string localImagePath, DateTimeOffset updatedAtUtc, bool isHighlighted = false)
        {
            if (!JournalRecordValidator.TryValidate(content, localImagePath, out var errorMessage))
                throw new ArgumentException(errorMessage);

            Content = content.Trim();
            ImageSource = imageSource;
            LocalImagePath = localImagePath;
            IsHighlighted = isHighlighted;
            UpdatedAtUtc = updatedAtUtc;
            SyncState = JournalSyncState.Pending;
            LastSyncError = null;
        }

        public void MarkForDeletion(DateTimeOffset updatedAtUtc)
        {
            IsDeleted = true;
            UpdatedAtUtc = updatedAtUtc;
            SyncState = JournalSyncState.Pending;
            LastSyncError = null;
        }
    }
}
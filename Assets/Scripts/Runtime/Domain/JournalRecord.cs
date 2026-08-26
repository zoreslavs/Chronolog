using System;

namespace Chronolog.Domain
{
    public sealed class JournalRecord
    {
        public Guid Id { get; }

        public string Content { get; }

        public JournalImageSource ImageSource { get; }

        public string LocalImagePath { get; }

        public string RemoteImageKey { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public DateTimeOffset? ServerReceivedAtUtc { get; private set; }

        public JournalSyncState SyncState { get; private set; }

        public string LastSyncError { get; private set; }

        private JournalRecord(Guid id, string content, JournalImageSource imageSource, string localImagePath, DateTimeOffset createdAtUtc)
        {
            Id = id;
            Content = content.Trim();
            ImageSource = imageSource;
            LocalImagePath = localImagePath;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = createdAtUtc;
            SyncState = JournalSyncState.Pending;
        }

        public static JournalRecord Create(Guid id, string content, JournalImageSource imageSource, string localImagePath, DateTimeOffset createdAtUtc)
        {
            if (!JournalRecordValidator.TryValidate(content, localImagePath, out var errorMessage))
                throw new ArgumentException(errorMessage);

            return new JournalRecord(id, content, imageSource, localImagePath, createdAtUtc);
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
            string lastSyncError)
        {
            if (!JournalRecordValidator.TryValidate(content, localImagePath, out var errorMessage))
                throw new ArgumentException(errorMessage);

            var record = new JournalRecord(id, content, imageSource, localImagePath, createdAtUtc)
            {
                RemoteImageKey = remoteImageKey,
                UpdatedAtUtc = updatedAtUtc,
                ServerReceivedAtUtc = serverReceivedAtUtc,
                SyncState = syncState,
                LastSyncError = lastSyncError
            };

            return record;
        }
    }
}

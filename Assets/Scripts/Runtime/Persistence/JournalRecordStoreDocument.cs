using System.Collections.Generic;
using System.Globalization;
using Chronolog.Domain;
using System.IO;
using System;

namespace Chronolog.Persistence
{
    [Serializable]
    internal sealed class JournalRecordStoreDocument
    {
        public int version = 1;
        public List<JournalRecordDocument> records = new();
    }

    [Serializable]
    internal sealed class JournalRecordDocument
    {
        public string id;
        public string content;
        public int imageSource;
        public string localImagePath;
        public string remoteImageKey;
        public string createdAtUtc;
        public string updatedAtUtc;
        public string serverReceivedAtUtc;
        public int syncState;
        public string lastSyncError;
        public bool isHighlighted;
        public bool isDeleted;

        public static JournalRecordDocument FromRecord(JournalRecord record)
        {
            return new JournalRecordDocument
            {
                id = record.Id.ToString("D"),
                content = record.Content,
                imageSource = (int)record.ImageSource,
                localImagePath = record.LocalImagePath,
                remoteImageKey = record.RemoteImageKey,
                createdAtUtc = record.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                updatedAtUtc = record.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                serverReceivedAtUtc = record.ServerReceivedAtUtc.HasValue
                    ? record.ServerReceivedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture)
                    : null,
                syncState = (int)record.SyncState,
                lastSyncError = record.LastSyncError,
                isHighlighted = record.IsHighlighted,
                isDeleted = record.IsDeleted
            };
        }

        public JournalRecord ToRecord()
        {
            if (!Guid.TryParse(id, out var recordId))
                throw new InvalidDataException("A stored journal record has an invalid ID.");

            if (!Enum.IsDefined(typeof(JournalImageSource), imageSource))
                throw new InvalidDataException("A stored journal record has an invalid image source.");

            if (!Enum.IsDefined(typeof(JournalSyncState), syncState))
                throw new InvalidDataException("A stored journal record has an invalid sync state.");

            if (!DateTimeOffset.TryParse(createdAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt))
                throw new InvalidDataException("A stored journal record has an invalid creation timestamp.");

            if (!DateTimeOffset.TryParse(updatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var updatedAt))
                throw new InvalidDataException("A stored journal record has an invalid update timestamp.");

            DateTimeOffset? serverReceivedAt = null;
            if (!string.IsNullOrEmpty(serverReceivedAtUtc))
            {
                if (!DateTimeOffset.TryParse(serverReceivedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedServerReceivedAt))
                {
                    throw new InvalidDataException("A stored journal record has an invalid server timestamp.");
                }

                serverReceivedAt = parsedServerReceivedAt;
            }

            return JournalRecord.Restore(
                recordId,
                content,
                (JournalImageSource)imageSource,
                localImagePath,
                ToOptionalValue(remoteImageKey),
                createdAt,
                updatedAt,
                serverReceivedAt,
                (JournalSyncState)syncState,
                ToOptionalValue(lastSyncError),
                isHighlighted,
                isDeleted);
        }

        private static string ToOptionalValue(string value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
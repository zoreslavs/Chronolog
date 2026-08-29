using Chronolog.Domain;
using NUnit.Framework;
using System;

namespace Chronolog.Tests
{
    public sealed class JournalRecordTests
    {
        private static readonly DateTimeOffset CreatedAtUtc = new(2026, 8, 26, 12, 30, 0, TimeSpan.Zero);

        [Test]
        public void TryValidate_RejectsWhitespaceOnlyContent()
        {
            var isValid = JournalRecordValidator.TryValidate("   ", "images/record.jpg", out var errorMessage);

            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Is.EqualTo("Content is required."));
        }

        [Test]
        public void TryValidate_RejectsMissingLocalImagePath()
        {
            var isValid = JournalRecordValidator.TryValidate("A note", "", out var errorMessage);

            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Is.EqualTo("An image is required."));
        }

        [Test]
        public void Create_InitializesAPendingRecordFromValidatedValues()
        {
            var id = Guid.Parse("d6d5ff93-48e1-4a8f-a494-d4dc9b386139");

            var record = JournalRecord.Create(
                id,
                "  First day of Chronolog.  ",
                JournalImageSource.Camera,
                "images/d6d5ff93.jpg",
                CreatedAtUtc);

            Assert.That(record.Id, Is.EqualTo(id));
            Assert.That(record.Content, Is.EqualTo("First day of Chronolog."));
            Assert.That(record.ImageSource, Is.EqualTo(JournalImageSource.Camera));
            Assert.That(record.LocalImagePath, Is.EqualTo("images/d6d5ff93.jpg"));
            Assert.That(record.CreatedAtUtc, Is.EqualTo(CreatedAtUtc));
            Assert.That(record.UpdatedAtUtc, Is.EqualTo(CreatedAtUtc));
            Assert.That(record.SyncState, Is.EqualTo(JournalSyncState.Pending));
            Assert.That(record.RemoteImageKey, Is.Null);
            Assert.That(record.ServerReceivedAtUtc, Is.Null);
            Assert.That(record.LastSyncError, Is.Null);
        }

        [Test]
        public void Create_RejectsAnInvalidRecord()
        {
            var exception = Assert.Throws<ArgumentException>(() => JournalRecord.Create(
                Guid.NewGuid(),
                " ",
                JournalImageSource.Gallery,
                "images/record.jpg",
                CreatedAtUtc));

            Assert.That(exception.Message, Is.EqualTo("Content is required."));
        }

        [Test]
        public void MarkSynced_StoresTheRemoteImageKeyAndHighlightState()
        {
            var record = JournalRecord.Create(
                Guid.Parse("d6d5ff93-48e1-4a8f-a494-d4dc9b386139"),
                "First day of Chronolog.",
                JournalImageSource.Camera,
                "images/d6d5ff93.jpg",
                CreatedAtUtc);
            var syncedAtUtc = CreatedAtUtc.AddMinutes(2);

            record.MarkSynced("images/d6d5ff93.jpg", true, syncedAtUtc);

            Assert.That(record.RemoteImageKey, Is.EqualTo("images/d6d5ff93.jpg"));
            Assert.That(record.IsHighlighted, Is.True);
            Assert.That(record.SyncState, Is.EqualTo(JournalSyncState.Synced));
            Assert.That(record.LastSyncError, Is.Null);
            Assert.That(record.UpdatedAtUtc, Is.EqualTo(syncedAtUtc));
        }

        [Test]
        public void MarkSyncing_ClearsThePreviousSyncErrorWithoutChangingTheEditDate()
        {
            var record = JournalRecord.Restore(
                Guid.Parse("d6d5ff93-48e1-4a8f-a494-d4dc9b386139"),
                "First day of Chronolog.",
                JournalImageSource.Camera,
                "images/d6d5ff93.jpg",
                null,
                CreatedAtUtc,
                CreatedAtUtc,
                null,
                JournalSyncState.Failed,
                "Network unavailable.");
            record.MarkSyncing();

            Assert.That(record.SyncState, Is.EqualTo(JournalSyncState.Syncing));
            Assert.That(record.LastSyncError, Is.Null);
            Assert.That(record.UpdatedAtUtc, Is.EqualTo(CreatedAtUtc));
        }

        [Test]
        public void MarkFailed_StoresTheFailureMessageWithoutChangingTheEditDate()
        {
            var record = JournalRecord.Create(
                Guid.Parse("d6d5ff93-48e1-4a8f-a494-d4dc9b386139"),
                "First day of Chronolog.",
                JournalImageSource.Camera,
                "images/d6d5ff93.jpg",
                CreatedAtUtc);
            record.MarkFailed("Network unavailable.");

            Assert.That(record.SyncState, Is.EqualTo(JournalSyncState.Failed));
            Assert.That(record.LastSyncError, Is.EqualTo("Network unavailable."));
            Assert.That(record.UpdatedAtUtc, Is.EqualTo(CreatedAtUtc));
        }

        [Test]
        public void Update_ReplacesTheEditableValuesAndQueuesTheExistingRecordForSync()
        {
            var record = JournalRecord.Create(
                Guid.Parse("d6d5ff93-48e1-4a8f-a494-d4dc9b386139"),
                "First day of Chronolog.",
                JournalImageSource.Camera,
                "images/original.jpg",
                CreatedAtUtc);
            record.MarkSynced("images/android-a1b2c3d4e5f60708/original.jpg", false, CreatedAtUtc.AddMinutes(1));
            var updatedAtUtc = CreatedAtUtc.AddMinutes(2);

            record.Update("Updated entry", JournalImageSource.Gallery, "images/replacement.png", updatedAtUtc);

            Assert.That(record.Content, Is.EqualTo("Updated entry"));
            Assert.That(record.ImageSource, Is.EqualTo(JournalImageSource.Gallery));
            Assert.That(record.LocalImagePath, Is.EqualTo("images/replacement.png"));
            Assert.That(record.RemoteImageKey, Is.EqualTo("images/android-a1b2c3d4e5f60708/original.jpg"));
            Assert.That(record.SyncState, Is.EqualTo(JournalSyncState.Pending));
            Assert.That(record.UpdatedAtUtc, Is.EqualTo(updatedAtUtc));
        }

        [Test]
        public void MarkForDeletion_HidesTheRecordAndQueuesItsRemoteDeletion()
        {
            var record = JournalRecord.Create(
                Guid.Parse("d6d5ff93-48e1-4a8f-a494-d4dc9b386139"),
                "First day of Chronolog.",
                JournalImageSource.Camera,
                "images/original.jpg",
                CreatedAtUtc);
            var deletedAtUtc = CreatedAtUtc.AddMinutes(2);

            record.MarkForDeletion(deletedAtUtc);

            Assert.That(record.IsDeleted, Is.True);
            Assert.That(record.SyncState, Is.EqualTo(JournalSyncState.Pending));
            Assert.That(record.UpdatedAtUtc, Is.EqualTo(deletedAtUtc));
            Assert.That(record.LastSyncError, Is.Null);
        }
    }
}

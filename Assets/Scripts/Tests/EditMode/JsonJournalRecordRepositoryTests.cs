using Chronolog.Persistence;
using Chronolog.Domain;
using NUnit.Framework;
using System.Linq;
using System.IO;
using System;

namespace Chronolog.Tests
{
    public sealed class JsonJournalRecordRepositoryTests
    {
        private string storageDirectoryPath;
        private string storageFilePath;

        [SetUp]
        public void SetUp()
        {
            storageDirectoryPath = Path.Combine(Path.GetTempPath(), "chronolog-tests", Guid.NewGuid().ToString("N"));
            storageFilePath = Path.Combine(storageDirectoryPath, "journal-records.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(storageDirectoryPath))
            {
                Directory.Delete(storageDirectoryPath, true);
            }
        }

        [Test]
        public void GetAll_ReturnsEmptyWhenStorageFileDoesNotExist()
        {
            var repository = new JsonJournalRecordRepository(storageFilePath);
            var records = repository.GetAll();

            Assert.That(records, Is.Empty);
        }

        [Test]
        public void Save_RestoresAllRecordValuesFromDisk()
        {
            var id = Guid.Parse("a3ee9967-5bf8-462d-ae3c-9f082e82ccae");
            var createdAtUtc = new DateTimeOffset(2026, 8, 26, 9, 0, 0, TimeSpan.Zero);
            var updatedAtUtc = new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);
            var serverReceivedAtUtc = new DateTimeOffset(2026, 8, 26, 10, 1, 0, TimeSpan.Zero);
            var record = JournalRecord.Restore(
                id,
                "Synced entry",
                JournalImageSource.Gallery,
                "images/a3ee9967.jpg",
                "entries/a3ee9967.jpg",
                createdAtUtc,
                updatedAtUtc,
                serverReceivedAtUtc,
                JournalSyncState.Synced,
                null,
                true);

            new JsonJournalRecordRepository(storageFilePath).Save(record);

            var restoredRecord = new JsonJournalRecordRepository(storageFilePath).GetAll().Single();

            Assert.That(restoredRecord.Id, Is.EqualTo(id));
            Assert.That(restoredRecord.Content, Is.EqualTo("Synced entry"));
            Assert.That(restoredRecord.ImageSource, Is.EqualTo(JournalImageSource.Gallery));
            Assert.That(restoredRecord.LocalImagePath, Is.EqualTo("images/a3ee9967.jpg"));
            Assert.That(restoredRecord.RemoteImageKey, Is.EqualTo("entries/a3ee9967.jpg"));
            Assert.That(restoredRecord.CreatedAtUtc, Is.EqualTo(createdAtUtc));
            Assert.That(restoredRecord.UpdatedAtUtc, Is.EqualTo(updatedAtUtc));
            Assert.That(restoredRecord.ServerReceivedAtUtc, Is.EqualTo(serverReceivedAtUtc));
            Assert.That(restoredRecord.SyncState, Is.EqualTo(JournalSyncState.Synced));
            Assert.That(restoredRecord.LastSyncError, Is.Null);
            Assert.That(restoredRecord.IsHighlighted, Is.True);
        }

        [Test]
        public void Save_ReplacesAnExistingRecordWithTheSameId()
        {
            var id = Guid.Parse("74187b41-6d72-4c8a-8c8e-12c735dd7d58");
            var createdAtUtc = new DateTimeOffset(2026, 8, 26, 9, 0, 0, TimeSpan.Zero);
            var repository = new JsonJournalRecordRepository(storageFilePath);
            var original = JournalRecord.Create(
                id,
                "Original entry",
                JournalImageSource.Camera,
                "images/74187b41.jpg",
                createdAtUtc);
            var replacement = JournalRecord.Restore(
                id,
                "Updated entry",
                JournalImageSource.Camera,
                "images/74187b41.jpg",
                null,
                createdAtUtc,
                createdAtUtc.AddMinutes(5),
                null,
                JournalSyncState.Failed,
                "Network unavailable.");

            repository.Save(original);
            repository.Save(replacement);

            var records = repository.GetAll();

            Assert.That(records.Count, Is.EqualTo(1));
            Assert.That(records[0].Content, Is.EqualTo("Updated entry"));
            Assert.That(records[0].SyncState, Is.EqualTo(JournalSyncState.Failed));
            Assert.That(records[0].LastSyncError, Is.EqualTo("Network unavailable."));
        }

        [Test]
        public void GetAll_ReturnsNewestRecordsFirst()
        {
            var repository = new JsonJournalRecordRepository(storageFilePath);
            var olderRecord = JournalRecord.Create(
                Guid.Parse("d0ec8c32-6477-4a7e-af89-709d7bf2ff49"),
                "Older entry",
                JournalImageSource.Camera,
                "images/older.jpg",
                new DateTimeOffset(2026, 8, 25, 9, 0, 0, TimeSpan.Zero));
            var newerRecord = JournalRecord.Create(
                Guid.Parse("76fc1a96-a78e-4a1a-a6b5-d42024bc6760"),
                "Newer entry",
                JournalImageSource.Gallery,
                "images/newer.jpg",
                new DateTimeOffset(2026, 8, 26, 9, 0, 0, TimeSpan.Zero));

            repository.Save(olderRecord);
            repository.Save(newerRecord);

            var records = repository.GetAll();

            Assert.That(records.Select(record => record.Id), Is.EqualTo(new[] { newerRecord.Id, olderRecord.Id }));
        }

        [Test]
        public void Delete_RemovesOnlyTheRequestedRecord()
        {
            var repository = new JsonJournalRecordRepository(storageFilePath);
            var firstRecord = JournalRecord.Create(
                Guid.Parse("d0ec8c32-6477-4a7e-af89-709d7bf2ff49"),
                "First entry",
                JournalImageSource.Camera,
                "images/first.jpg",
                new DateTimeOffset(2026, 8, 25, 9, 0, 0, TimeSpan.Zero));
            var secondRecord = JournalRecord.Create(
                Guid.Parse("76fc1a96-a78e-4a1a-a6b5-d42024bc6760"),
                "Second entry",
                JournalImageSource.Gallery,
                "images/second.jpg",
                new DateTimeOffset(2026, 8, 26, 9, 0, 0, TimeSpan.Zero));
            repository.Save(firstRecord);
            repository.Save(secondRecord);

            repository.Delete(firstRecord.Id);

            Assert.That(repository.GetAll().Select(record => record.Id), Is.EqualTo(new[] { secondRecord.Id }));
        }
    }
}

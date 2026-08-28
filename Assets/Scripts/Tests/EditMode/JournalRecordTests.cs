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
    }
}
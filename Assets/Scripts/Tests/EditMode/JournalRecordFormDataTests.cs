using Chronolog.Domain;
using Chronolog.Presentation;
using NUnit.Framework;
using System;

namespace Chronolog.Tests
{
    public sealed class JournalRecordFormDataTests
    {
        [Test]
        public void CanSave_IsFalseUntilContentAndImageHaveBeenSet()
        {
            var formData = new JournalRecordFormData();

            Assert.That(formData.CanSave, Is.False);

            formData.SetContent("A calm afternoon walk.");

            Assert.That(formData.CanSave, Is.False);

            formData.SetImage("images/2799dc7a.jpg", JournalImageSource.Gallery);

            Assert.That(formData.CanSave, Is.True);
        }

        [Test]
        public void Clear_RemovesTheEnteredContentAndImage()
        {
            var formData = new JournalRecordFormData();
            formData.SetContent("A calm afternoon walk.");
            formData.SetImage("images/2799dc7a.jpg", JournalImageSource.Gallery);

            formData.Clear();

            Assert.That(formData.Content, Is.Null);
            Assert.That(formData.LocalImagePath, Is.Null);
            Assert.That(formData.ImageSource, Is.Null);
            Assert.That(formData.CanSave, Is.False);
        }

        [Test]
        public void CreateRecord_UsesTheCompletedFormValues()
        {
            var formData = new JournalRecordFormData();
            var recordId = Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9");
            var createdAtUtc = new DateTimeOffset(2026, 8, 27, 9, 30, 0, TimeSpan.Zero);
            formData.SetContent("A calm afternoon walk.");
            formData.SetImage("images/2799dc7a.jpg", JournalImageSource.Gallery);

            var record = formData.CreateRecord(recordId, createdAtUtc);
            Assert.That(record.Id, Is.EqualTo(recordId));
            Assert.That(record.Content, Is.EqualTo("A calm afternoon walk."));
            Assert.That(record.ImageSource, Is.EqualTo(JournalImageSource.Gallery));
            Assert.That(record.LocalImagePath, Is.EqualTo("images/2799dc7a.jpg"));
            Assert.That(record.CreatedAtUtc, Is.EqualTo(createdAtUtc));
        }

        [Test]
        public void CreateRecord_UsesTheSelectedHighlightState()
        {
            var formData = new JournalRecordFormData();
            formData.SetContent("A calm afternoon walk.");
            formData.SetImage("images/2799dc7a.jpg", JournalImageSource.Gallery);
            formData.SetHighlighted(true);

            var record = formData.CreateRecord(Guid.NewGuid(), DateTimeOffset.UtcNow);

            Assert.That(record.IsHighlighted, Is.True);
        }

        [Test]
        public void Load_UpdatesTheExistingRecordInsteadOfCreatingANewOne()
        {
            var recordId = Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9");
            var createdAtUtc = new DateTimeOffset(2026, 8, 27, 9, 30, 0, TimeSpan.Zero);
            var existingRecord = JournalRecord.Create(
                recordId,
                "Original entry",
                JournalImageSource.Camera,
                "images/original.jpg",
                createdAtUtc);
            existingRecord.MarkSynced("images/android-a1b2c3d4e5f60708/original.jpg", false, createdAtUtc);
            var formData = new JournalRecordFormData();

            formData.Load(existingRecord);
            formData.SetContent("Updated entry");
            formData.SetImage("images/replacement.png", JournalImageSource.Gallery);
            var savedRecord = formData.CreateRecord(Guid.NewGuid(), createdAtUtc.AddMinutes(5));

            Assert.That(savedRecord, Is.SameAs(existingRecord));
            Assert.That(savedRecord.Id, Is.EqualTo(recordId));
            Assert.That(savedRecord.CreatedAtUtc, Is.EqualTo(createdAtUtc));
            Assert.That(savedRecord.Content, Is.EqualTo("Updated entry"));
            Assert.That(savedRecord.LocalImagePath, Is.EqualTo("images/replacement.png"));
            Assert.That(savedRecord.SyncState, Is.EqualTo(JournalSyncState.Pending));
        }
    }
}

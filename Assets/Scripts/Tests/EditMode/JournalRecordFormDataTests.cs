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
    }
}

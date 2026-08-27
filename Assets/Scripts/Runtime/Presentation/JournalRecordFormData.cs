using Chronolog.Domain;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalRecordFormData
    {
        public string Content { get; private set; }

        public string LocalImagePath { get; private set; }

        public JournalImageSource? ImageSource { get; private set; }

        public bool CanSave => JournalRecordValidator.TryValidate(Content, LocalImagePath, out _);

        public void SetContent(string content)
        {
            Content = content;
        }

        public void SetImage(string localImagePath, JournalImageSource imageSource)
        {
            LocalImagePath = localImagePath;
            ImageSource = imageSource;
        }

        public JournalRecord CreateRecord(Guid recordId, DateTimeOffset createdAtUtc)
        {
            if (!ImageSource.HasValue)
                throw new InvalidOperationException("An image source is required.");

            return JournalRecord.Create(recordId, Content, ImageSource.Value, LocalImagePath, createdAtUtc);
        }

        public void Clear()
        {
            Content = null;
            LocalImagePath = null;
            ImageSource = null;
        }
    }
}

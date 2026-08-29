using Chronolog.Domain;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalRecordFormData
    {
        private JournalRecord editingRecord;

        public string Content { get; private set; }

        public string LocalImagePath { get; private set; }

        public JournalImageSource? ImageSource { get; private set; }

        public bool IsHighlighted { get; private set; }

        public bool CanSave => JournalRecordValidator.TryValidate(Content, LocalImagePath, out _);

        public bool IsEditing => editingRecord != null;

        public JournalRecord EditingRecord => editingRecord;

        public void SetContent(string content)
        {
            Content = content;
        }

        public void SetImage(string localImagePath, JournalImageSource imageSource)
        {
            LocalImagePath = localImagePath;
            ImageSource = imageSource;
        }

        public void SetHighlighted(bool isHighlighted)
        {
            IsHighlighted = isHighlighted;
        }

        public void Load(JournalRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            editingRecord = record;
            Content = record.Content;
            LocalImagePath = record.LocalImagePath;
            ImageSource = record.ImageSource;
            IsHighlighted = record.IsHighlighted;
        }

        public JournalRecord CreateRecord(Guid recordId, DateTimeOffset createdAtUtc)
        {
            if (!ImageSource.HasValue)
                throw new InvalidOperationException("An image source is required.");

            if (editingRecord != null)
            {
                editingRecord.Update(Content, ImageSource.Value, LocalImagePath, createdAtUtc, IsHighlighted);
                return editingRecord;
            }

            return JournalRecord.Create(recordId, Content, ImageSource.Value, LocalImagePath, createdAtUtc, IsHighlighted);
        }

        public void Clear()
        {
            Content = null;
            LocalImagePath = null;
            ImageSource = null;
            IsHighlighted = false;
            editingRecord = null;
        }
    }
}
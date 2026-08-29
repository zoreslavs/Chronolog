using Chronolog.Domain;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalListRecordView : MonoBehaviour
    {
        private const int ThumbnailMaxSize = 512;

        [SerializeField] private Text dateLabel;
        [SerializeField] private Text contentLabel;
        [SerializeField] private Image imageBackgroud;
        [SerializeField] private RawImage imagePreview;
        [SerializeField] private Text imageSourceLabel;
        [SerializeField] private Button selectButton;
        [SerializeField] private Color highlightColor;

        private Texture2D imagePreviewTexture;
        private Action<JournalRecord> recordSelected;
        private JournalRecord record;

        public void Init(JournalRecord record)
        {
            Init(record, null);
        }

        public void Init(JournalRecord record, Action<JournalRecord> recordSelected)
        {
            this.record = record ?? throw new ArgumentNullException(nameof(record));
            this.recordSelected = recordSelected;
            dateLabel.text = GetDateLabelText(record);
            contentLabel.text = record.Content;
            imageSourceLabel.text = record.ImageSource.ToString();

            if (record.IsHighlighted && imageBackgroud != null)
                imageBackgroud.color = highlightColor;

            selectButton.onClick.AddListener(Select);

            SetImagePreview(record.LocalImagePath);
        }

        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(Select);

            if (imagePreviewTexture != null)
                Destroy(imagePreviewTexture);
        }

        private void Select()
        {
            recordSelected?.Invoke(record);
        }

        private static string GetDateLabelText(JournalRecord record)
        {
            var createdAt = record.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy · HH:mm");
            if (record.UpdatedAtUtc == record.CreatedAtUtc)
                return createdAt;

            var updatedAt = record.UpdatedAtUtc.ToLocalTime().ToString("dd MMM yyyy · HH:mm");
            return $"{createdAt} | Edited {updatedAt}";
        }

        private void SetImagePreview(string localImagePath)
        {
            if (imagePreview == null)
                return;

            if (imagePreviewTexture != null)
            {
                Destroy(imagePreviewTexture);
                imagePreviewTexture = null;
            }

            if (string.IsNullOrWhiteSpace(localImagePath) || !File.Exists(localImagePath))
            {
                imagePreview.texture = null;
                imagePreview.gameObject.SetActive(false);
                return;
            }

            imagePreviewTexture = NativeGallery.LoadImageAtPath(localImagePath, ThumbnailMaxSize);
            imagePreview.texture = imagePreviewTexture;
            imagePreview.uvRect = GetSquareCropRect(imagePreviewTexture);
            imagePreview.gameObject.SetActive(imagePreviewTexture != null);
        }

        private static Rect GetSquareCropRect(Texture2D texture)
        {
            if (texture == null || texture.width == texture.height)
                return new Rect(0f, 0f, 1f, 1f);

            if (texture.width > texture.height)
            {
                var width = (float)texture.height / texture.width;
                return new Rect((1f - width) / 2f, 0f, width, 1f);
            }

            var height = (float)texture.width / texture.height;
            return new Rect(0f, (1f - height) / 2f, 1f, height);
        }
    }
}
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
        [SerializeField] private Image imagePreview;
        [SerializeField] private RawImage imageForeground;
        [SerializeField] private Text imageSourceLabel;
        [SerializeField] private Image imageSourceIcon;
        [SerializeField] private Sprite cameraIcon;
        [SerializeField] private Sprite galleryIcon;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject highlightBadge;
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
            SetImageSourceIcon(record.ImageSource);

            highlightBadge.SetActive(record.IsHighlighted);

            if (record.IsHighlighted)
                imageBackgroud.color = highlightColor;

            selectButton.onClick.AddListener(Select);

            SetImagePreview(record.LocalImagePath);
        }

        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(Select);

            ClearImagePreviewTexture();
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

            ClearImagePreviewTexture();

            if (string.IsNullOrWhiteSpace(localImagePath) || !File.Exists(localImagePath))
            {
                imagePreview.gameObject.SetActive(false);
                return;
            }

            imagePreviewTexture = NativeGallery.LoadImageAtPath(localImagePath, ThumbnailMaxSize);

            if (imagePreviewTexture == null)
            {
                imagePreview.gameObject.SetActive(false);
                return;
            }

            imageForeground.texture = imagePreviewTexture;
            imageForeground.uvRect = new Rect(0f, 0f, 1f, 1f);
            SetForegroundSize(imagePreviewTexture);

            imagePreview.gameObject.SetActive(imagePreviewTexture != null);
        }

        private void SetImageSourceIcon(JournalImageSource imageSource)
        {
            if (imageSourceIcon == null)
                return;

            imageSourceIcon.sprite = imageSource == JournalImageSource.Camera ? cameraIcon : galleryIcon;
            imageSourceIcon.gameObject.SetActive(imageSourceIcon.sprite != null);
        }

        private void ClearImagePreviewTexture()
        {
            if (imagePreviewTexture == null)
                return;

            Destroy(imagePreviewTexture);
            imagePreviewTexture = null;
        }

        private void SetForegroundSize(Texture2D texture)
        {
            var parentSize = imagePreview.rectTransform.rect.size;
            var scale = Mathf.Min(parentSize.x / texture.width, parentSize.y / texture.height);
            imageForeground.rectTransform.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
        }

    }
}
using Chronolog.Persistence;
using Chronolog.Domain;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalRecordFormScreen : MonoBehaviour
    {
        private const int FormImagePreviewMaxSize = 1024;

        [SerializeField] private InputField contentInput;
        [SerializeField] private GameObject imagePlaceholder;
        [SerializeField] private RawImage imageForeground;
        [SerializeField] private Image imagePreview;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Toggle highlightToggle;
        [SerializeField] private JournalScreenNavigator navigator;
        [SerializeField] private JournalSyncService syncService;
        [SerializeField] private JournalDeleteRecordPopup deletePopup;

        private readonly JournalRecordFormData formData = new();
        private Texture2D imagePreviewTexture;

        private void OnEnable()
        {
            contentInput.onValueChanged.AddListener(SetContent);
            cancelButton.onClick.AddListener(Cancel);
            saveButton.onClick.AddListener(Save);
            deleteButton.onClick.AddListener(ShowDeletePopup);
            highlightToggle.onValueChanged.AddListener(SetHighlighted);

            deletePopup.OnDeleteConfirmed = confirmed =>
            {
                if (confirmed)
                    ConfirmDelete();
                else
                    CancelDelete();
            };

            ResetForm();
        }

        private void OnDisable()
        {
            contentInput.onValueChanged.RemoveListener(SetContent);
            cancelButton.onClick.RemoveListener(Cancel);
            saveButton.onClick.RemoveListener(Save);
            deleteButton.onClick.RemoveListener(ShowDeletePopup);
            highlightToggle.onValueChanged.RemoveListener(SetHighlighted);

            deletePopup.OnDeleteConfirmed = null;
        }

        public void SetContent(string content)
        {
            formData.SetContent(content);
            RefreshSaveButton();
        }

        public void SetImage(string localImagePath, JournalImageSource imageSource)
        {
            formData.SetImage(localImagePath, imageSource);
            ShowImagePreview(localImagePath);
            RefreshSaveButton();
        }

        public void SetHighlighted(bool isHighlighted)
        {
            formData.SetHighlighted(isHighlighted);
        }

        public void Cancel()
        {
            ResetForm();
            navigator.ShowList();
        }

        public void Open(JournalRecord record)
        {
            formData.Load(record);
            contentInput.SetTextWithoutNotify(formData.Content);
            highlightToggle.SetIsOnWithoutNotify(formData.IsHighlighted);

            ShowImagePreview(formData.LocalImagePath);
            RefreshDeleteButton();
            RefreshSaveButton();
        }

        public void Save()
        {
            if (!formData.CanSave)
                return;

            var journalRecord = formData.CreateRecord(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var repository = new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath());
            repository.Save(journalRecord);
            syncService?.Sync(journalRecord);
            navigator.ShowList();
        }

        public void ShowDeletePopup()
        {
            if (formData.IsEditing)
                deletePopup.gameObject.SetActive(true);
        }

        public void CancelDelete()
        {
            deletePopup.gameObject.SetActive(false);
        }

        public void ConfirmDelete()
        {
            if (!formData.IsEditing)
                return;

            var record = formData.EditingRecord;
            record.MarkForDeletion(DateTimeOffset.UtcNow);
            var repository = new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath());
            repository.Save(record);
            syncService?.Sync(record);
            ResetForm();
            navigator.ShowList();
        }

        private void ResetForm()
        {
            formData.Clear();
            contentInput.SetTextWithoutNotify(string.Empty);
            highlightToggle.SetIsOnWithoutNotify(false);

            ClearImagePreview();
            CancelDelete();
            RefreshDeleteButton();
            RefreshSaveButton();
        }

        private void ShowImagePreview(string localImagePath)
        {
            ClearImagePreview();
            imagePreviewTexture = NativeGallery.LoadImageAtPath(localImagePath, FormImagePreviewMaxSize);

            if (imagePreview == null || imagePreviewTexture == null)
                return;

            imageForeground.texture = imagePreviewTexture;
            imageForeground.gameObject.SetActive(true);
            SetForegroundSize(imagePreviewTexture);

            GetImagePlaceholderLabel().SetActive(false);
        }

        private void ClearImagePreview()
        {
            imageForeground.texture = null;
            imageForeground.gameObject.SetActive(false);

            if (imagePreviewTexture != null)
            {
                Destroy(imagePreviewTexture);
                imagePreviewTexture = null;
            }

            GetImagePlaceholderLabel().SetActive(true);
        }

        private GameObject GetImagePlaceholderLabel()
        {
            var placeholderLabel = imagePlaceholder.GetComponentInChildren<Text>(true);
            return placeholderLabel != null ? placeholderLabel.gameObject : imagePlaceholder;
        }

        private void SetForegroundSize(Texture2D texture)
        {
            var parentSize = imagePreview.rectTransform.rect.size;
            var scale = Mathf.Min(parentSize.x / texture.width, parentSize.y / texture.height);
            imageForeground.rectTransform.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
        }

        private void RefreshSaveButton()
        {
            saveButton.interactable = formData.CanSave;
        }

        private void RefreshDeleteButton()
        {
            deleteButton.gameObject.SetActive(formData.IsEditing);
        }
    }
}
using Chronolog.Persistence;
using Chronolog.Domain;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalRecordFormScreen : MonoBehaviour
    {
        private const int ImagePreviewMaxSize = 1024;

        [SerializeField] private InputField contentInput;
        [SerializeField] private GameObject imagePlaceholder;
        [SerializeField] private Button cancelButton;
        [SerializeField] private RawImage imagePreview;
        [SerializeField] private JournalScreenNavigator navigator;
        [SerializeField] private Button saveButton;

        private readonly JournalRecordFormData formData = new();
        private Texture2D imagePreviewTexture;

        private void OnEnable()
        {
            contentInput.onValueChanged.AddListener(SetContent);
            cancelButton.onClick.AddListener(Cancel);
            saveButton.onClick.AddListener(Save);
            ResetForm();
        }

        private void OnDisable()
        {
            contentInput.onValueChanged.RemoveListener(SetContent);
            cancelButton.onClick.RemoveListener(Cancel);
            saveButton.onClick.RemoveListener(Save);
        }

        public void SetContent(string content)
        {
            formData.SetContent(content);
            RefreshSaveButton();
        }

        public void SetImage(string localImagePath, JournalImageSource imageSource)
        {
            formData.SetImage(localImagePath, imageSource);
            RefreshSaveButton();
        }

        public void Cancel()
        {
            ResetForm();
            navigator.ShowList();
        }

        public void Save()
        {
            if (!formData.CanSave)
                return;

            var journalRecord = formData.CreateRecord(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var repository = new JsonJournalRecordRepository(JournalRecordStoragePath.GetRecordsFilePath());
            repository.Save(journalRecord);
            navigator.ShowList();
        }

        private void ResetForm()
        {
            formData.Clear();
            contentInput.SetTextWithoutNotify(string.Empty);
            ClearImagePreview();
            RefreshSaveButton();
        }

        private void ClearImagePreview()
        {
            if (imagePreview != null)
            {
                imagePreview.texture = null;
                imagePreview.gameObject.SetActive(false);
            }

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

        private void RefreshSaveButton()
        {
            saveButton.interactable = formData.CanSave;
        }
    }
}
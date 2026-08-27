using Chronolog.Persistence;
using Chronolog.Domain;
using UnityEngine.UI;
using UnityEngine;

namespace Chronolog.Presentation
{
    public sealed class JournalDeviceMedia : MonoBehaviour
    {
        [SerializeField] private Button chooseImageButton;
        [SerializeField] private JournalRecordFormScreen formScreen;
        [SerializeField] private Button takePhotoButton;

        private JournalImageSelectionHandler imageSelectionHandler;

        private void Awake()
        {
            var imageStorage = new JournalImageStorage(Application.persistentDataPath);
            imageSelectionHandler = new JournalImageSelectionHandler(imageStorage, formScreen.SetImage);
        }

        private void OnEnable()
        {
            takePhotoButton.onClick.AddListener(TakePhoto);
            chooseImageButton.onClick.AddListener(ChooseImage);
        }

        private void OnDisable()
        {
            takePhotoButton.onClick.RemoveListener(TakePhoto);
            chooseImageButton.onClick.RemoveListener(ChooseImage);
        }

        private void TakePhoto()
        {
            if (NativeCamera.IsCameraBusy())
                return;

            NativeCamera.TakePicture(path => HandleImageSelection(path, JournalImageSource.Camera), 2048);
        }

        private void ChooseImage()
        {
            if (NativeGallery.IsMediaPickerBusy())
                return;

            NativeGallery.GetImageFromGallery(path => HandleImageSelection(path, JournalImageSource.Gallery));
        }

        private void HandleImageSelection(string sourceImagePath, JournalImageSource imageSource)
        {
            try
            {
                imageSelectionHandler.Complete(sourceImagePath, imageSource);
            }
            catch (System.IO.IOException exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
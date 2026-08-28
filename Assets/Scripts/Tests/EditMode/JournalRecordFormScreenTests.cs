using Chronolog.Domain;
using Chronolog.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Chronolog.Tests
{
    public sealed class JournalRecordFormScreenTests
    {
        [Test]
        public void SetImage_EnablesSaveAfterContentHasBeenEntered()
        {
            var formObject = new GameObject("Journal Record Form");
            formObject.SetActive(false);
            var formScreen = formObject.AddComponent<JournalRecordFormScreen>();
            var saveButton = CreateButton("Save", formObject.transform);
            var imagePlaceholder = new GameObject("Image Placeholder");
            imagePlaceholder.transform.SetParent(formObject.transform, false);
            var serializedFormScreen = new SerializedObject(formScreen);
            serializedFormScreen.FindProperty("saveButton").objectReferenceValue = saveButton;
            serializedFormScreen.FindProperty("imagePlaceholder").objectReferenceValue = imagePlaceholder;
            serializedFormScreen.ApplyModifiedPropertiesWithoutUndo();
            var imagePath = CreateImageFile();

            formScreen.SetContent("A calm afternoon walk.");
            formScreen.SetImage(imagePath, JournalImageSource.Gallery);

            Assert.That(saveButton.interactable, Is.True);

            File.Delete(imagePath);
            Object.DestroyImmediate(formObject);
        }

        [Test]
        public void SetImage_ShowsAPreviewAndKeepsTheImageSectionVisible()
        {
            var formObject = new GameObject("Journal Record Form");
            formObject.SetActive(false);
            var formScreen = formObject.AddComponent<JournalRecordFormScreen>();
            var imageSection = new GameObject("Journal Image");
            imageSection.transform.SetParent(formObject.transform, false);
            var imagePlaceholder = new GameObject("Placeholder");
            imagePlaceholder.transform.SetParent(imageSection.transform, false);
            var placeholderLabel = new GameObject("Label", typeof(Text));
            placeholderLabel.transform.SetParent(imagePlaceholder.transform, false);
            var previewObject = new GameObject("Preview", typeof(RawImage));
            previewObject.transform.SetParent(imageSection.transform, false);
            previewObject.SetActive(false);
            var imagePreview = previewObject.GetComponent<RawImage>();
            var saveButton = CreateButton("Save", formObject.transform);
            var imagePath = CreateImageFile();
            var serializedFormScreen = new SerializedObject(formScreen);
            serializedFormScreen.FindProperty("imagePlaceholder").objectReferenceValue = imagePlaceholder;
            var imagePreviewProperty = serializedFormScreen.FindProperty("imagePreview");

            if (imagePreviewProperty != null)
                imagePreviewProperty.objectReferenceValue = imagePreview;

            serializedFormScreen.FindProperty("saveButton").objectReferenceValue = saveButton;
            serializedFormScreen.ApplyModifiedPropertiesWithoutUndo();

            formScreen.SetImage(imagePath, JournalImageSource.Gallery);

            Assert.That(imageSection.activeSelf, Is.True);
            Assert.That(placeholderLabel.activeSelf, Is.False);
            Assert.That(imagePreview.texture, Is.Not.Null);

            File.Delete(imagePath);
            Object.DestroyImmediate(formObject);
        }

        [Test]
        public void Save_ReturnsToTheJournalList()
        {
            var listScreen = new GameObject("List Screen");
            listScreen.SetActive(false);
            var formObject = new GameObject("Journal Record Form");
            formObject.SetActive(false);
            var formScreen = formObject.AddComponent<JournalRecordFormScreen>();
            var navigatorObject = new GameObject("Navigator");
            var navigator = navigatorObject.AddComponent<JournalScreenNavigator>();
            var contentInput = CreateInputField("Content", formObject.transform);
            var imagePlaceholder = new GameObject("Image Placeholder");
            imagePlaceholder.transform.SetParent(formObject.transform, false);
            var cancelButton = CreateButton("Cancel", formObject.transform);
            var saveButton = CreateButton("Save", formObject.transform);
            ConfigureNavigator(navigator, listScreen, formScreen);
            ConfigureForm(formScreen, contentInput, imagePlaceholder, cancelButton, saveButton, navigator);
            var imagePath = CreateImageFile();
            formScreen.SetContent("A calm afternoon walk.");
            formScreen.SetImage(imagePath, JournalImageSource.Gallery);

            formScreen.Save();

            Assert.That(listScreen.activeSelf, Is.True);
            Assert.That(formObject.activeSelf, Is.False);

            File.Delete(imagePath);
            Object.DestroyImmediate(navigatorObject);
            Object.DestroyImmediate(listScreen);
            Object.DestroyImmediate(formObject);
        }

        [Test]
        public void Cancel_ReturnsToTheJournalList()
        {
            var listScreen = new GameObject("List Screen");
            listScreen.SetActive(false);
            var formObject = new GameObject("Journal Record Form");
            var formScreen = formObject.AddComponent<JournalRecordFormScreen>();
            var navigatorObject = new GameObject("Navigator");
            var navigator = navigatorObject.AddComponent<JournalScreenNavigator>();
            var contentInput = CreateInputField("Content", formObject.transform);
            var imagePlaceholder = new GameObject("Image Placeholder");
            imagePlaceholder.transform.SetParent(formObject.transform, false);
            var saveButton = CreateButton("Save", formObject.transform);
            ConfigureNavigator(navigator, listScreen, formScreen);
            ConfigureForm(formScreen, contentInput, imagePlaceholder, null, saveButton, navigator);

            formScreen.Cancel();

            Assert.That(listScreen.activeSelf, Is.True);
            Assert.That(formObject.activeSelf, Is.False);

            Object.DestroyImmediate(navigatorObject);
            Object.DestroyImmediate(listScreen);
            Object.DestroyImmediate(formObject);
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }

        private static InputField CreateInputField(string name, Transform parent)
        {
            var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            return inputObject.GetComponent<InputField>();
        }

        private static void ConfigureForm(
            JournalRecordFormScreen formScreen,
            InputField contentInput,
            GameObject imagePlaceholder,
            Button cancelButton,
            Button saveButton,
            JournalScreenNavigator navigator)
        {
            var serializedFormScreen = new SerializedObject(formScreen);
            serializedFormScreen.FindProperty("navigator").objectReferenceValue = navigator;
            serializedFormScreen.FindProperty("contentInput").objectReferenceValue = contentInput;
            serializedFormScreen.FindProperty("imagePlaceholder").objectReferenceValue = imagePlaceholder;
            serializedFormScreen.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            serializedFormScreen.FindProperty("saveButton").objectReferenceValue = saveButton;
            serializedFormScreen.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureNavigator(JournalScreenNavigator navigator, GameObject listScreen, JournalRecordFormScreen formScreen)
        {
            var serializedNavigator = new SerializedObject(navigator);
            serializedNavigator.FindProperty("listScreen").objectReferenceValue = listScreen;
            serializedNavigator.FindProperty("formScreen").objectReferenceValue = formScreen.gameObject;
            serializedNavigator.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string CreateImageFile()
        {
            var imagePath = Path.Combine(Path.GetTempPath(), $"{System.Guid.NewGuid():N}.png");
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.green, Color.green, Color.green, Color.green });
            File.WriteAllBytes(imagePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return imagePath;
        }
    }
}

using Chronolog.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Chronolog.Tests
{
    public sealed class JournalScreenNavigatorTests
    {
        [Test]
        public void ShowForm_HidesTheListAndShowsTheForm()
        {
            var listScreen = new GameObject("List Screen");
            var formScreen = new GameObject("Form Screen");
            formScreen.SetActive(false);
            var journalRecordFormScreen = formScreen.AddComponent<JournalRecordFormScreen>();
            ConfigureForm(journalRecordFormScreen, formScreen.transform);
            var navigatorObject = new GameObject("Navigator");
            var navigator = navigatorObject.AddComponent<JournalScreenNavigator>();
            var serializedNavigator = new SerializedObject(navigator);
            serializedNavigator.FindProperty("listScreen").objectReferenceValue = listScreen;
            serializedNavigator.FindProperty("formScreen").objectReferenceValue = formScreen;
            serializedNavigator.ApplyModifiedPropertiesWithoutUndo();

            navigator.ShowForm();

            Assert.That(listScreen.activeSelf, Is.False);
            Assert.That(formScreen.activeSelf, Is.True);

            Object.DestroyImmediate(navigatorObject);
            Object.DestroyImmediate(listScreen);
            Object.DestroyImmediate(formScreen);
        }

        private static void ConfigureForm(JournalRecordFormScreen formScreen, Transform parent)
        {
            var contentInput = CreateInputField("Content", parent);
            var imagePlaceholder = new GameObject("Image Placeholder");
            imagePlaceholder.transform.SetParent(parent, false);
            var cancelButton = CreateButton("Cancel", parent);
            var saveButton = CreateButton("Save", parent);
            var serializedFormScreen = new SerializedObject(formScreen);
            serializedFormScreen.FindProperty("contentInput").objectReferenceValue = contentInput;
            serializedFormScreen.FindProperty("imagePlaceholder").objectReferenceValue = imagePlaceholder;
            serializedFormScreen.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            serializedFormScreen.FindProperty("saveButton").objectReferenceValue = saveButton;
            serializedFormScreen.ApplyModifiedPropertiesWithoutUndo();
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
    }
}

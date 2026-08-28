using NUnit.Framework;
using Chronolog.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Chronolog.Tests
{
    public sealed class JournalListSceneTests
    {
        [Test]
        public void MainScene_UsesTheUICanvasForBothScreens()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var journalListScreen = Object.FindFirstObjectByType<JournalListScreen>();
            var formScreen = Object.FindFirstObjectByType<JournalRecordFormScreen>(FindObjectsInactive.Include);
            var uiCanvas = GameObject.Find("UI").GetComponent<Canvas>();

            Assert.That(journalListScreen, Is.Not.Null);
            var serializedJournalListScreen = new SerializedObject(journalListScreen);
            var recordViewPrefab = serializedJournalListScreen.FindProperty("recordViewPrefab").objectReferenceValue as JournalListRecordView;

            Assert.That(formScreen, Is.Not.Null);
            Assert.That(uiCanvas, Is.Not.Null);
            Assert.That(journalListScreen.GetComponent<Canvas>(), Is.Null);
            Assert.That(formScreen.GetComponent<Canvas>(), Is.Null);
            Assert.That(recordViewPrefab, Is.Not.Null);
        }

        [Test]
        public void MainScene_ContainsAnInactiveJournalRecordForm()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

            var formScreen = Object.FindFirstObjectByType<JournalRecordFormScreen>(FindObjectsInactive.Include);

            Assert.That(formScreen, Is.Not.Null);
            Assert.That(formScreen.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MainScene_UsesAJournalRecordFormSceneObject()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

            var formScreen = Object.FindFirstObjectByType<JournalRecordFormScreen>(FindObjectsInactive.Include);

            Assert.That(PrefabUtility.IsPartOfPrefabInstance(formScreen), Is.False);
        }

        [Test]
        public void MainScene_ConnectsDeviceMediaToTheJournalRecordForm()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

            var deviceMedia = Object.FindFirstObjectByType<JournalDeviceMedia>(FindObjectsInactive.Include);

            Assert.That(deviceMedia, Is.Not.Null);
            var serializedDeviceMedia = new SerializedObject(deviceMedia);
            Assert.That(serializedDeviceMedia.FindProperty("formScreen").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedDeviceMedia.FindProperty("takePhotoButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedDeviceMedia.FindProperty("chooseImageButton").objectReferenceValue, Is.Not.Null);
        }

    }
}

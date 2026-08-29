using System;
using Chronolog.Domain;
using Chronolog.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Chronolog.Tests
{
    public sealed class JournalListRecordViewTests
    {
        [Test]
        public void Init_DisplaysTheRecordValues()
        {
            var root = new GameObject("Record View");
            var recordView = root.AddComponent<JournalListRecordView>();
            var dateLabel = CreateLabel("Date", root.transform);
            var contentLabel = CreateLabel("Content", root.transform);
            var imageSourceLabel = CreateLabel("Image Source", root.transform);
            var selectButton = CreateButton("Select", root.transform);
            var serializedRecordView = new SerializedObject(recordView);
            serializedRecordView.FindProperty("dateLabel").objectReferenceValue = dateLabel;
            serializedRecordView.FindProperty("contentLabel").objectReferenceValue = contentLabel;
            serializedRecordView.FindProperty("imageSourceLabel").objectReferenceValue = imageSourceLabel;
            serializedRecordView.FindProperty("selectButton").objectReferenceValue = selectButton;
            serializedRecordView.ApplyModifiedPropertiesWithoutUndo();
            var record = JournalRecord.Create(
                Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9"),
                "A calm afternoon walk.",
                JournalImageSource.Gallery,
                "images/2799dc7a.jpg",
                new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));

            recordView.Init(record);

            Assert.That(dateLabel.text, Is.EqualTo(record.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy · HH:mm")));
            Assert.That(contentLabel.text, Is.EqualTo(record.Content));
            Assert.That(imageSourceLabel.text, Is.EqualTo(record.ImageSource.ToString()));

            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Init_DisplaysTheEditedDateAfterTheCreatedDate()
        {
            var root = new GameObject("Record View");
            var recordView = root.AddComponent<JournalListRecordView>();
            var dateLabel = CreateLabel("Date", root.transform);
            var contentLabel = CreateLabel("Content", root.transform);
            var imageSourceLabel = CreateLabel("Image Source", root.transform);
            var selectButton = CreateButton("Select", root.transform);
            var serializedRecordView = new SerializedObject(recordView);
            serializedRecordView.FindProperty("dateLabel").objectReferenceValue = dateLabel;
            serializedRecordView.FindProperty("contentLabel").objectReferenceValue = contentLabel;
            serializedRecordView.FindProperty("imageSourceLabel").objectReferenceValue = imageSourceLabel;
            serializedRecordView.FindProperty("selectButton").objectReferenceValue = selectButton;
            serializedRecordView.ApplyModifiedPropertiesWithoutUndo();
            var createdAtUtc = new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero);
            var updatedAtUtc = createdAtUtc.AddDays(1);
            var record = JournalRecord.Create(
                Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9"),
                "Updated afternoon walk.",
                JournalImageSource.Gallery,
                "images/2799dc7a.jpg",
                createdAtUtc);
            record.Update("Updated afternoon walk.", JournalImageSource.Gallery, "images/2799dc7a.jpg", updatedAtUtc);

            recordView.Init(record);

            Assert.That(dateLabel.text, Is.EqualTo($"{createdAtUtc.ToLocalTime():dd MMM yyyy · HH:mm} | Edited {updatedAtUtc.ToLocalTime():dd MMM yyyy · HH:mm}"));
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Init_DisplaysTheRecordImage()
        {
            var root = new GameObject("Record View");
            var recordView = root.AddComponent<JournalListRecordView>();
            var dateLabel = CreateLabel("Date", root.transform);
            var contentLabel = CreateLabel("Content", root.transform);
            var imageSourceLabel = CreateLabel("Image Source", root.transform);
            var selectButton = CreateButton("Select", root.transform);
            var thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(RawImage));
            thumbnailObject.transform.SetParent(root.transform, false);
            thumbnailObject.SetActive(false);
            var thumbnail = thumbnailObject.GetComponent<RawImage>();
            var serializedRecordView = new SerializedObject(recordView);
            serializedRecordView.FindProperty("dateLabel").objectReferenceValue = dateLabel;
            serializedRecordView.FindProperty("contentLabel").objectReferenceValue = contentLabel;
            serializedRecordView.FindProperty("imageSourceLabel").objectReferenceValue = imageSourceLabel;
            serializedRecordView.FindProperty("selectButton").objectReferenceValue = selectButton;
            var imagePreviewProperty = serializedRecordView.FindProperty("imagePreview");
            Assert.That(imagePreviewProperty, Is.Not.Null, "The record view should have an image preview field.");
            imagePreviewProperty.objectReferenceValue = thumbnail;
            serializedRecordView.ApplyModifiedPropertiesWithoutUndo();
            var imagePath = CreateImageFile();
            var record = JournalRecord.Create(
                Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9"),
                "A calm afternoon walk.",
                JournalImageSource.Gallery,
                imagePath,
                new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));

            recordView.Init(record);

            Assert.That(thumbnail.gameObject.activeSelf, Is.True);
            Assert.That(thumbnail.texture, Is.Not.Null);

            File.Delete(imagePath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Init_CropsWideImagesToTheSquareThumbnail()
        {
            var root = new GameObject("Record View");
            var recordView = root.AddComponent<JournalListRecordView>();
            var dateLabel = CreateLabel("Date", root.transform);
            var contentLabel = CreateLabel("Content", root.transform);
            var imageSourceLabel = CreateLabel("Image Source", root.transform);
            var selectButton = CreateButton("Select", root.transform);
            var thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(RawImage));
            thumbnailObject.transform.SetParent(root.transform, false);
            var thumbnail = thumbnailObject.GetComponent<RawImage>();
            var serializedRecordView = new SerializedObject(recordView);
            serializedRecordView.FindProperty("dateLabel").objectReferenceValue = dateLabel;
            serializedRecordView.FindProperty("contentLabel").objectReferenceValue = contentLabel;
            serializedRecordView.FindProperty("imageSourceLabel").objectReferenceValue = imageSourceLabel;
            serializedRecordView.FindProperty("selectButton").objectReferenceValue = selectButton;
            serializedRecordView.FindProperty("imagePreview").objectReferenceValue = thumbnail;
            serializedRecordView.ApplyModifiedPropertiesWithoutUndo();
            var imagePath = CreateImageFile(4, 2);
            var record = JournalRecord.Create(
                Guid.Parse("a94ae169-6f76-40e3-9b11-c6d0cd12f3ac"),
                "A calm afternoon walk.",
                JournalImageSource.Camera,
                imagePath,
                new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));

            recordView.Init(record);

            Assert.That(thumbnail.uvRect.x, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(thumbnail.uvRect.width, Is.EqualTo(0.5f).Within(0.001f));

            File.Delete(imagePath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Init_AppliesTheHighlightColorForAHighlightedRecord()
        {
            var root = new GameObject("Record View");
            var recordView = root.AddComponent<JournalListRecordView>();
            var dateLabel = CreateLabel("Date", root.transform);
            var contentLabel = CreateLabel("Content", root.transform);
            var imageSourceLabel = CreateLabel("Image Source", root.transform);
            var selectButton = CreateButton("Select", root.transform);
            var imageBackground = new GameObject("Image Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            imageBackground.transform.SetParent(root.transform, false);
            var serializedRecordView = new SerializedObject(recordView);
            serializedRecordView.FindProperty("dateLabel").objectReferenceValue = dateLabel;
            serializedRecordView.FindProperty("contentLabel").objectReferenceValue = contentLabel;
            serializedRecordView.FindProperty("imageSourceLabel").objectReferenceValue = imageSourceLabel;
            serializedRecordView.FindProperty("selectButton").objectReferenceValue = selectButton;
            serializedRecordView.FindProperty("imageBackgroud").objectReferenceValue = imageBackground;
            serializedRecordView.FindProperty("highlightColor").colorValue = Color.yellow;
            serializedRecordView.ApplyModifiedPropertiesWithoutUndo();
            var record = JournalRecord.Create(
                Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9"),
                "Highlighted entry",
                JournalImageSource.Gallery,
                "images/2799dc7a.jpg",
                new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));
            record.MarkSynced("images/android-a1b2c3d4e5f60708/2799dc7a.jpg", true, record.CreatedAtUtc);

            recordView.Init(record);

            Assert.That(imageBackground.color, Is.EqualTo(Color.yellow));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Text CreateLabel(string name, Transform parent)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent, false);
            return label.GetComponent<Text>();
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            return button.GetComponent<Button>();
        }

        private static string CreateImageFile()
        {
            return CreateImageFile(2, 2);
        }

        private static string CreateImageFile(int width, int height)
        {
            var imagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];

            for (var index = 0; index < pixels.Length; index++)
                pixels[index] = Color.green;

            texture.SetPixels(pixels);
            File.WriteAllBytes(imagePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return imagePath;
        }
    }
}

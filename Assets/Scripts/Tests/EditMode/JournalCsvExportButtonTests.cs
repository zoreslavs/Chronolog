using Chronolog.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Chronolog.Tests
{
    public sealed class JournalCsvExportButtonTests
    {
        [Test]
        public void Component_ExposesDependenciesForInspectorConfiguration()
        {
            var gameObject = new GameObject();
            var exportButton = gameObject.AddComponent<JournalCsvExportButton>();
            var serializedExportButton = new SerializedObject(exportButton);

            Assert.That(serializedExportButton.FindProperty("label"), Is.Not.Null);
            Assert.That(serializedExportButton.FindProperty("button"), Is.Not.Null);
            Assert.That(serializedExportButton.FindProperty("canvasGroup"), Is.Not.Null);
            Assert.That(serializedExportButton.FindProperty("exporter"), Is.Not.Null);
            Assert.That(serializedExportButton.FindProperty("syncService"), Is.Not.Null);

            Object.DestroyImmediate(gameObject);
        }
    }
}

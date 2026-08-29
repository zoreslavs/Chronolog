using Chronolog.Persistence;
using NUnit.Framework;
using System.IO;
using System;

namespace Chronolog.Tests
{
    public sealed class JournalImageStorageTests
    {
        private string storageDirectoryPath;

        [SetUp]
        public void SetUp()
        {
            storageDirectoryPath = Path.Combine(Path.GetTempPath(), "chronolog-tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(storageDirectoryPath))
            {
                Directory.Delete(storageDirectoryPath, true);
            }
        }

        [Test]
        public void CopyToLocalStorage_CopiesTheImageToTheImagesDirectory()
        {
            var sourceImagePath = Path.Combine(storageDirectoryPath, "source.jpg");
            var sourceImageBytes = new byte[] { 4, 8, 15, 16, 23, 42 };
            Directory.CreateDirectory(storageDirectoryPath);
            File.WriteAllBytes(sourceImagePath, sourceImageBytes);
            var imageStorage = new JournalImageStorage(storageDirectoryPath);

            var localImagePath = imageStorage.CopyToLocalStorage(sourceImagePath);

            Assert.That(Path.GetDirectoryName(localImagePath), Is.EqualTo(Path.Combine(storageDirectoryPath, "images")));
            Assert.That(Path.GetExtension(localImagePath), Is.EqualTo(".jpg"));
            Assert.That(File.ReadAllBytes(localImagePath), Is.EqualTo(sourceImageBytes));
        }

        [Test]
        public void SaveToLocalStorage_WritesDownloadedImageBytesWithTheRequestedExtension()
        {
            var imageStorage = new JournalImageStorage(storageDirectoryPath);
            var imageBytes = new byte[] { 4, 8, 15, 16, 23, 42 };

            var localImagePath = imageStorage.SaveToLocalStorage(imageBytes, ".png");

            Assert.That(Path.GetDirectoryName(localImagePath), Is.EqualTo(Path.Combine(storageDirectoryPath, "images")));
            Assert.That(Path.GetExtension(localImagePath), Is.EqualTo(".png"));
            Assert.That(File.ReadAllBytes(localImagePath), Is.EqualTo(imageBytes));
        }
    }
}

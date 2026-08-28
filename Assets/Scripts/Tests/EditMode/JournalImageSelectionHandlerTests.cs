using Chronolog.Domain;
using Chronolog.Persistence;
using Chronolog.Presentation;
using NUnit.Framework;
using System.IO;
using System;

namespace Chronolog.Tests
{
    public sealed class JournalImageSelectionHandlerTests
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
        public void Complete_CopiesTheSelectedImageAndPublishesItsLocalPath()
        {
            var sourceImagePath = Path.Combine(storageDirectoryPath, "selected.jpg");
            var sourceImageBytes = new byte[] { 4, 8, 15, 16, 23, 42 };
            Directory.CreateDirectory(storageDirectoryPath);
            File.WriteAllBytes(sourceImagePath, sourceImageBytes);
            string localImagePath = null;
            JournalImageSource? imageSource = null;
            var imageStorage = new JournalImageStorage(storageDirectoryPath);
            var handler = new JournalImageSelectionHandler(imageStorage, (path, source) =>
            {
                localImagePath = path;
                imageSource = source;
            });

            handler.Complete(sourceImagePath, JournalImageSource.Gallery);

            Assert.That(imageSource, Is.EqualTo(JournalImageSource.Gallery));
            Assert.That(File.ReadAllBytes(localImagePath), Is.EqualTo(sourceImageBytes));
        }
    }
}

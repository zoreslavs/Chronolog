using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalImageContentTypeTests
    {
        [TestCase("images/photo.jpg", "image/jpeg")]
        [TestCase("images/photo.jpeg", "image/jpeg")]
        [TestCase("images/photo.png", "image/png")]
        public void GetForFilePath_ReturnsTheSupportedContentType(string filePath, string expectedContentType)
        {
            var contentType = JournalImageContentType.GetForFilePath(filePath);

            Assert.That(contentType, Is.EqualTo(expectedContentType));
        }

        [Test]
        public void GetForFilePath_RejectsAnUnsupportedExtension()
        {
            var exception = Assert.Throws<System.ArgumentException>(() => JournalImageContentType.GetForFilePath("images/photo.webp"));

            Assert.That(exception.ParamName, Is.EqualTo("filePath"));
            Assert.That(exception.Message, Does.StartWith("Only JPEG and PNG images are supported."));
        }
    }
}

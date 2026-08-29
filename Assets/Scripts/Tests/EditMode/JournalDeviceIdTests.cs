using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalDeviceIdTests
    {
        [Test]
        public void Get_ReturnsTheSameEditorFallbackIdOnRepeatedCalls()
        {
            var firstId = JournalDeviceId.Get();
            var secondId = JournalDeviceId.Get();

            Assert.That(firstId, Does.StartWith("editor-"));
            Assert.That(secondId, Is.EqualTo(firstId));
        }
    }
}

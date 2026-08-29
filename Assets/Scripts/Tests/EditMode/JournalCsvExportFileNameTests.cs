using System;
using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalCsvExportFileNameTests
    {
        [Test]
        public void Create_UsesTimestampedChronologCsvFileName()
        {
            var fileName = JournalCsvExportFileName.Create(new DateTimeOffset(2026, 8, 28, 21, 30, 45, TimeSpan.Zero));

            Assert.That(fileName, Is.EqualTo("chronolog-2026-08-28-213045.csv"));
        }
    }
}

using System;
using System.IO;
using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalCsvExportFileTests
    {
        [Test]
        public void Save_CreatesTimestampedCsvFileWithContent()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                var filePath = JournalCsvExportFile.Save(
                    directoryPath,
                    "id,content\n1,An entry",
                    new DateTimeOffset(2026, 8, 28, 21, 30, 45, TimeSpan.Zero));

                Assert.That(Path.GetFileName(filePath), Is.EqualTo("chronolog-2026-08-28-213045.csv"));
                Assert.That(File.ReadAllText(filePath), Is.EqualTo("id,content\n1,An entry"));
            }
            finally
            {
                if (Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, true);
            }
        }
    }
}

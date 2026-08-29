using System.IO;
using System;

namespace Chronolog.Presentation
{
    public static class JournalCsvExportFile
    {
        public static string Save(string directoryPath, string content, DateTimeOffset timestamp)
        {
            Directory.CreateDirectory(directoryPath);
            var filePath = Path.Combine(directoryPath, JournalCsvExportFileName.Create(timestamp));
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
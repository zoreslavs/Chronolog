using System;

namespace Chronolog.Presentation
{
    public static class JournalCsvExportFileName
    {
        public static string Create(DateTimeOffset timestamp)
        {
            return $"chronolog-{timestamp:yyyy-MM-dd-HHmmss}.csv";
        }
    }
}
using UnityEngine;
using System.IO;

namespace Chronolog.Presentation
{
    public static class JournalRecordStoragePath
    {
        private const string RecordsFileName = "journal-records.json";

        public static string GetRecordsFilePath()
        {
            return Path.Combine(Application.persistentDataPath, RecordsFileName);
        }
    }
}
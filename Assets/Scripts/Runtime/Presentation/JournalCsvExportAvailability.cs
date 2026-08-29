namespace Chronolog.Presentation
{
    public static class JournalCsvExportAvailability
    {
        public static bool CanExport(JournalSyncStatus syncStatus, bool hasRecords)
        {
            return syncStatus == JournalSyncStatus.Synced && hasRecords;
        }
    }
}
namespace Chronolog.Presentation
{
    public static class JournalListScrollAvailability
    {
        public static bool CanScroll(JournalSyncStatus syncStatus)
        {
            return syncStatus != JournalSyncStatus.Syncing;
        }
    }
}
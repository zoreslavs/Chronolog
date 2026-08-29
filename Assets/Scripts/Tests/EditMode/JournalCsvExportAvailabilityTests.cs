using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalCsvExportAvailabilityTests
    {
        [TestCase(JournalSyncStatus.Synced, true, true)]
        [TestCase(JournalSyncStatus.Synced, false, false)]
        [TestCase(JournalSyncStatus.Syncing, true, false)]
        [TestCase(JournalSyncStatus.Offline, true, false)]
        [TestCase(JournalSyncStatus.Failed, true, false)]
        public void CanExport_RequiresCompletedSyncAndRecords(JournalSyncStatus status, bool hasRecords, bool expected)
        {
            var canExport = JournalCsvExportAvailability.CanExport(status, hasRecords);

            Assert.That(canExport, Is.EqualTo(expected));
        }
    }
}

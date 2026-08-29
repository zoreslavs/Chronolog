using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalListScrollAvailabilityTests
    {
        [TestCase(JournalSyncStatus.Syncing, false)]
        [TestCase(JournalSyncStatus.Synced, true)]
        [TestCase(JournalSyncStatus.Offline, true)]
        [TestCase(JournalSyncStatus.Failed, true)]
        public void CanScroll_DisablesScrollingOnlyWhileSyncing(JournalSyncStatus status, bool expected)
        {
            var canScroll = JournalListScrollAvailability.CanScroll(status);

            Assert.That(canScroll, Is.EqualTo(expected));
        }
    }
}

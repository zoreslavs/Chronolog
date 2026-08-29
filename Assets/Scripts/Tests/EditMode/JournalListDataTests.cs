using System;
using System.Collections.Generic;
using Chronolog.Domain;
using Chronolog.Presentation;
using NUnit.Framework;

namespace Chronolog.Tests
{
    public sealed class JournalListDataTests
    {
        [Test]
        public void Create_ShowsEmptyStateWhenRepositoryHasNoRecords()
        {
            var journalListData = JournalListData.Create(new FakeJournalRecordRepository());

            Assert.That(journalListData.IsEmpty, Is.True);
            Assert.That(journalListData.EmptyStateMessage, Is.EqualTo("Your journal is waiting for its first entry."));
            Assert.That(journalListData.Records, Is.Empty);
        }

        [Test]
        public void Create_ExposesRecordsForTheJournalList()
        {
            var record = JournalRecord.Create(
                Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9"),
                "A calm afternoon walk.",
                JournalImageSource.Gallery,
                "images/2799dc7a.jpg",
                new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));
            var journalListData = JournalListData.Create(new FakeJournalRecordRepository(record));

            Assert.That(journalListData.IsEmpty, Is.False);
            Assert.That(journalListData.Records, Is.EqualTo(new[] { record }));
        }

        [Test]
        public void Create_ExcludesRecordsMarkedForDeletion()
        {
            var visibleRecord = JournalRecord.Create(
                Guid.Parse("2799dc7a-8cdc-4127-8a90-6e67b6abe7d9"),
                "Visible entry",
                JournalImageSource.Gallery,
                "images/visible.jpg",
                new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));
            var deletedRecord = JournalRecord.Create(
                Guid.Parse("50b6c53c-4b80-44e1-8e20-1f8bc0bf2e32"),
                "Deleted entry",
                JournalImageSource.Gallery,
                "images/deleted.jpg",
                new DateTimeOffset(2026, 8, 26, 16, 30, 0, TimeSpan.Zero));
            deletedRecord.MarkForDeletion(new DateTimeOffset(2026, 8, 26, 17, 30, 0, TimeSpan.Zero));

            var journalListData = JournalListData.Create(new FakeJournalRecordRepository(visibleRecord, deletedRecord));

            Assert.That(journalListData.Records, Is.EqualTo(new[] { visibleRecord }));
        }

        private sealed class FakeJournalRecordRepository : IJournalRecordRepository
        {
            private readonly IReadOnlyList<JournalRecord> records;

            public FakeJournalRecordRepository(params JournalRecord[] records)
            {
                this.records = records;
            }

            public IReadOnlyList<JournalRecord> GetAll()
            {
                return records;
            }

            public void Save(JournalRecord record)
            {
                throw new NotSupportedException();
            }

            public void Delete(Guid recordId)
            {
                throw new NotSupportedException();
            }
        }
    }
}

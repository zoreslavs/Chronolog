using System.Collections.Generic;
using Chronolog.Domain;
using System.Linq;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalListData
    {
        public IReadOnlyList<JournalRecord> Records { get; }

        public bool IsEmpty => Records.Count == 0;

        public string EmptyStateMessage => "Your journal is waiting for its first entry.";


        private JournalListData(IReadOnlyList<JournalRecord> records)
        {
            Records = records;
        }

        public static JournalListData Create(IJournalRecordRepository repository)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            return new JournalListData(repository.GetAll().Where(record => !record.IsDeleted).ToArray());
        }
    }
}
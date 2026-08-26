using System.Collections.Generic;

namespace Chronolog.Domain
{
    public interface IJournalRecordRepository
    {
        IReadOnlyList<JournalRecord> GetAll();

        void Save(JournalRecord record);
    }
}
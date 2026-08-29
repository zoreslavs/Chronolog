using System.Collections.Generic;
using System;

namespace Chronolog.Domain
{
    public interface IJournalRecordRepository
    {
        IReadOnlyList<JournalRecord> GetAll();

        void Save(JournalRecord record);

        void Delete(Guid recordId);
    }
}
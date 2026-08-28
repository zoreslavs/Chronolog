namespace Chronolog.Server.Core;

public interface IJournalRecordStore
{
    Task SaveAsync(RemoteJournalRecord record);
    Task<RemoteJournalRecord?> GetAsync(string deviceId, string recordId);
    Task<IReadOnlyList<RemoteJournalRecord>> ListAsync(string deviceId);
    Task DeleteAsync(string deviceId, string recordId);
}
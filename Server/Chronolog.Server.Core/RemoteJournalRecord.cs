namespace Chronolog.Server.Core;

public sealed record RemoteJournalRecord(
    string DeviceId,
    string Id,
    string CreatedAtUtc,
    string UpdatedAtUtc,
    string Content,
    string ImageSource,
    string ImageKey,
    bool IsHighlighted);
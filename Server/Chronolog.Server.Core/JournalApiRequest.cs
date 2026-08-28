namespace Chronolog.Server.Core;

public sealed record JournalApiRequest(string RouteKey, string? Body, string? DeviceId = null, string? RecordId = null);
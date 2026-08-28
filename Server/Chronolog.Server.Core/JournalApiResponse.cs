namespace Chronolog.Server.Core;

public sealed record JournalApiResponse(int StatusCode, IReadOnlyDictionary<string, string> Headers, string Body);
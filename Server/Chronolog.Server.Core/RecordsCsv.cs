namespace Chronolog.Server.Core;

public static class RecordsCsv
{
    public static string Create(IEnumerable<RemoteJournalRecord> records)
    {
        const string header = "id,createdAtUtc,updatedAtUtc,content,imageSource,imageKey,isHighlighted";
        var rows = records.Select(record => string.Join(",", new[]
        {
            Escape(record.Id),
            Escape(record.CreatedAtUtc),
            Escape(record.UpdatedAtUtc),
            Escape(record.Content),
            Escape(record.ImageSource),
            Escape(record.ImageKey),
            record.IsHighlighted ? "true" : "false"
        }));

        return string.Join("\n", new[] { header }.Concat(rows));
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
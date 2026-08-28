using Chronolog.Server.Core;
using Xunit;

namespace Chronolog.Server.Tests;

public sealed class RecordsCsvTests
{
    [Fact]
    public void Create_ExportsRecordMetadataAsOneCsvTable()
    {
        var csv = RecordsCsv.Create(new[]
        {
            new RemoteJournalRecord(
                "android-a1b2c3d4e5f60708",
                "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9",
                "2026-08-27T09:30:00.000Z",
                "2026-08-27T10:30:00.000Z",
                "Morning walk, sunny",
                "Gallery",
                "images/2799dc7a.jpg",
                true)
        });

        var expected = string.Join("\n", new[]
        {
            "id,createdAtUtc,updatedAtUtc,content,imageSource,imageKey,isHighlighted",
            "2799dc7a-8cdc-4127-8a90-6e67b6abe7d9,2026-08-27T09:30:00.000Z,2026-08-27T10:30:00.000Z,\"Morning walk, sunny\",Gallery,images/2799dc7a.jpg,true"
        });

        Assert.Equal(expected, csv);
    }
}

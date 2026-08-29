using Amazon.DynamoDBv2.Model;
using Chronolog.Server.Api;
using Xunit;

namespace Chronolog.Server.Tests;

public sealed class DynamoDbQueryPagerTests
{
    [Fact]
    public async Task ReadAllAsync_ReadsEveryDynamoDbQueryPage()
    {
        var responses = new Queue<QueryResponse>([
            new QueryResponse
            {
                Items = [CreateItem("older")],
                LastEvaluatedKey = new Dictionary<string, AttributeValue>
                {
                    ["deviceId"] = new() { S = "android-a1b2c3d4e5f60708" },
                    ["id"] = new() { S = "older" }
                }
            },
            new QueryResponse { Items = [CreateItem("newer")] }
        ]);
        var queryStartKeys = new List<Dictionary<string, AttributeValue>?>();

        var items = await DynamoDbQueryPager.ReadAllAsync(startKey =>
        {
            queryStartKeys.Add(startKey);
            return Task.FromResult(responses.Dequeue());
        });

        Assert.Equal(["older", "newer"], items.Select(item => item["id"].S));
        Assert.Equal(2, queryStartKeys.Count);
        Assert.Null(queryStartKeys[0]);
        Assert.Equal("older", queryStartKeys[1]!["id"].S);
    }

    [Fact]
    public async Task ReadAllAsync_ReturnsAnEmptyListWhenDynamoDbOmitsItems()
    {
        var items = await DynamoDbQueryPager.ReadAllAsync(_ => Task.FromResult(new QueryResponse { Items = null! }));

        Assert.Empty(items);
    }

    private static Dictionary<string, AttributeValue> CreateItem(string id)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["deviceId"] = new() { S = "android-a1b2c3d4e5f60708" },
            ["id"] = new() { S = id }
        };
    }
}

using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Amazon;
using Chronolog.Server.Api;
using Xunit;

namespace Chronolog.Server.Tests;

public sealed class DynamoDbJournalRecordStoreTests
{
    [Fact]
    public async Task GetAsync_ReturnsNullWhenDynamoDbDoesNotFindTheRecord()
    {
        var dynamoDb = CreateDynamoDb(new GetItemResponse { Item = null! });
        var store = new DynamoDbJournalRecordStore(dynamoDb, "records");

        var record = await store.GetAsync("android-a1b2c3d4e5f60708", "dfe85285-53c8-4364-ad71-c3f6adc255e6");

        Assert.Null(record);
    }

    [Fact]
    public async Task ListAsync_ReadsLegacyRecordsWithoutTheOptionalHighlightField()
    {
        var dynamoDb = CreateDynamoDb(queryResponse: new QueryResponse
        {
            Items = [CreateRecordItem()]
        });
        var store = new DynamoDbJournalRecordStore(dynamoDb, "records");

        var record = Assert.Single(await store.ListAsync("android-a1b2c3d4e5f60708"));

        Assert.False(record.IsHighlighted);
    }

    private static IAmazonDynamoDB CreateDynamoDb(GetItemResponse? getItemResponse = null, QueryResponse? queryResponse = null)
    {
        return new TestDynamoDbClient(
            getItemResponse ?? new GetItemResponse(),
            queryResponse ?? new QueryResponse());
    }

    private static Dictionary<string, AttributeValue> CreateRecordItem()
    {
        return new Dictionary<string, AttributeValue>
        {
            ["deviceId"] = new() { S = "android-a1b2c3d4e5f60708" },
            ["id"] = new() { S = "dfe85285-53c8-4364-ad71-c3f6adc255e6" },
            ["createdAtUtc"] = new() { S = "2026-08-29T07:32:12.495Z" },
            ["updatedAtUtc"] = new() { S = "2026-08-29T07:32:12.495Z" },
            ["content"] = new() { S = "A legacy record" },
            ["imageSource"] = new() { S = "Gallery" },
            ["imageKey"] = new() { S = "images/android-a1b2c3d4e5f60708/dfe85285-53c8-4364-ad71-c3f6adc255e6.jpg" }
        };
    }

    private sealed class TestDynamoDbClient : AmazonDynamoDBClient
    {
        private readonly GetItemResponse getItemResponse;
        private readonly QueryResponse queryResponse;

        public TestDynamoDbClient(GetItemResponse getItemResponse, QueryResponse queryResponse) : base(RegionEndpoint.EUCentral1)
        {
            this.getItemResponse = getItemResponse;
            this.queryResponse = queryResponse;
        }

        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(getItemResponse);
        }

        public override Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(queryResponse);
        }
    }
}

using Chronolog.Server.Core;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;

namespace Chronolog.Server.Api;

public sealed class DynamoDbJournalRecordStore : IJournalRecordStore
{
    private readonly IAmazonDynamoDB dynamoDb;
    private readonly string tableName;

    public DynamoDbJournalRecordStore(IAmazonDynamoDB dynamoDb, string tableName)
    {
        this.dynamoDb = dynamoDb;
        this.tableName = tableName;
    }

    public async Task SaveAsync(RemoteJournalRecord record)
    {
        await dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["deviceId"] = new() { S = record.DeviceId },
                ["id"] = new() { S = record.Id },
                ["createdAtUtc"] = new() { S = record.CreatedAtUtc },
                ["updatedAtUtc"] = new() { S = record.UpdatedAtUtc },
                ["content"] = new() { S = record.Content },
                ["imageSource"] = new() { S = record.ImageSource },
                ["imageKey"] = new() { S = record.ImageKey },
                ["isHighlighted"] = new() { BOOL = record.IsHighlighted }
            }
        });
    }

    public async Task<RemoteJournalRecord?> GetAsync(string deviceId, string recordId)
    {
        var response = await dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = tableName,
            Key = GetKey(deviceId, recordId)
        });

        return response.Item is not { Count: > 0 } ? null : ToRecord(response.Item);
    }

    public async Task<IReadOnlyList<RemoteJournalRecord>> ListAsync(string deviceId)
    {
        var items = await DynamoDbQueryPager.ReadAllAsync(startKey => dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = tableName,
            KeyConditionExpression = "#deviceId = :deviceId",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#deviceId"] = "deviceId" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":deviceId"] = new() { S = deviceId } },
            ExclusiveStartKey = startKey
        }));

        return items
            .Select(ToRecord)
            .OrderByDescending(record => record.CreatedAtUtc)
            .ToList();
    }

    public async Task DeleteAsync(string deviceId, string recordId)
    {
        await dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = tableName,
            Key = GetKey(deviceId, recordId)
        });
    }

    private static Dictionary<string, AttributeValue> GetKey(string deviceId, string recordId)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["deviceId"] = new() { S = deviceId },
            ["id"] = new() { S = recordId }
        };
    }

    private static RemoteJournalRecord ToRecord(IReadOnlyDictionary<string, AttributeValue> item)
    {
        return new RemoteJournalRecord(
            item["deviceId"].S,
            item["id"].S,
            item["createdAtUtc"].S,
            item["updatedAtUtc"].S,
            item["content"].S,
            item["imageSource"].S,
            item["imageKey"].S,
            item.TryGetValue("isHighlighted", out var isHighlighted) && isHighlighted.BOOL == true);
    }
}

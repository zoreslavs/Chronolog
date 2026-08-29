using Amazon.DynamoDBv2.Model;

namespace Chronolog.Server.Api;

public static class DynamoDbQueryPager
{
    public static async Task<IReadOnlyList<Dictionary<string, AttributeValue>>> ReadAllAsync(
        Func<Dictionary<string, AttributeValue>?, Task<QueryResponse>> queryPage)
    {
        var items = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? startKey = null;

        do
        {
            var response = await queryPage(startKey);
            items.AddRange(response.Items ?? []);
            startKey = response.LastEvaluatedKey is { Count: > 0 } ? response.LastEvaluatedKey : null;
        }
        while (startKey != null);

        return items;
    }
}

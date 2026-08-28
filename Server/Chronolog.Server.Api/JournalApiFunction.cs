using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.APIGatewayEvents;
using Chronolog.Server.Core;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.S3;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Chronolog.Server.Api;

public sealed class JournalApiFunction
{
    private readonly JournalApi journalApi;

    public JournalApiFunction() : this(CreateJournalApi())
    {
    }

    public JournalApiFunction(JournalApi journalApi)
    {
        this.journalApi = journalApi;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        string? deviceId = null;
        string? recordId = null;
        request.Headers?.TryGetValue("x-chronolog-device-id", out deviceId);
        request.PathParameters?.TryGetValue("id", out recordId);
        var response = await journalApi.HandleAsync(new JournalApiRequest(request.RouteKey, request.Body, deviceId, recordId));

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = response.StatusCode,
            Headers = response.Headers.ToDictionary(),
            Body = response.Body
        };
    }

    private static JournalApi CreateJournalApi()
    {
        var tableName = GetRequiredEnvironmentVariable("RECORDS_TABLE_NAME");
        var bucketName = GetRequiredEnvironmentVariable("IMAGES_BUCKET_NAME");

        return new JournalApi(
            new DynamoDbJournalRecordStore(new AmazonDynamoDBClient(), tableName),
            new S3JournalImageUploader(new AmazonS3Client(), bucketName),
            Console.Error.WriteLine);
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Environment variable '{name}' is required.");
    }
}
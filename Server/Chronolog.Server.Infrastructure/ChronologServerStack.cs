using ApiHttpMethod = Amazon.CDK.AWS.Apigatewayv2.HttpMethod;
using DynamoDbAttribute = Amazon.CDK.AWS.DynamoDB.Attribute;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK;
using Constructs;

namespace Chronolog.Server.Infrastructure;

public sealed class ChronologServerStack : Stack
{
    public ChronologServerStack(Construct scope, string id, string apiCodePath, IStackProps? props = null)
        : base(scope, id, props)
    {
        var recordsTable = new Table(this, "RecordsTable", new TableProps
        {
            PartitionKey = new DynamoDbAttribute
            {
                Name = "deviceId",
                Type = AttributeType.STRING
            },
            SortKey = new DynamoDbAttribute
            {
                Name = "id",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY
        });
        var imagesBucket = new Bucket(this, "ImagesBucket", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            AutoDeleteObjects = true,
            RemovalPolicy = RemovalPolicy.DESTROY
        });
        var journalApiFunction = new Function(this, "JournalApiFunction", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Handler = "Chronolog.Server.Api::Chronolog.Server.Api.JournalApiFunction::FunctionHandler",
            Code = Code.FromAsset(apiCodePath),
            MemorySize = 512,
            Timeout = Duration.Seconds(15),
            Environment = new Dictionary<string, string>
            {
                ["RECORDS_TABLE_NAME"] = recordsTable.TableName,
                ["IMAGES_BUCKET_NAME"] = imagesBucket.BucketName
            }
        });

        recordsTable.GrantReadWriteData(journalApiFunction);
        imagesBucket.GrantReadWrite(journalApiFunction);

        var httpApi = new HttpApi(this, "JournalHttpApi", new HttpApiProps
        {
            CorsPreflight = new CorsPreflightOptions
            {
                AllowHeaders = new[] { "content-type", "x-chronolog-device-id" },
                AllowMethods = new[] { CorsHttpMethod.GET, CorsHttpMethod.POST, CorsHttpMethod.PUT, CorsHttpMethod.DELETE },
                AllowOrigins = new[] { "*" }
            }
        });
        var integration = new HttpLambdaIntegration("JournalApiIntegration", journalApiFunction);

        httpApi.AddRoutes(new AddRoutesOptions
        {
            Path = "/records",
            Methods = new[] { ApiHttpMethod.GET, ApiHttpMethod.POST },
            Integration = integration
        });
        httpApi.AddRoutes(new AddRoutesOptions
        {
            Path = "/records/{id}",
            Methods = new[] { ApiHttpMethod.PUT, ApiHttpMethod.DELETE },
            Integration = integration
        });
        httpApi.AddRoutes(new AddRoutesOptions
        {
            Path = "/records/{id}/image",
            Methods = new[] { ApiHttpMethod.GET },
            Integration = integration
        });
        httpApi.AddRoutes(new AddRoutesOptions
        {
            Path = "/uploads",
            Methods = new[] { ApiHttpMethod.POST },
            Integration = integration
        });
        httpApi.AddRoutes(new AddRoutesOptions
        {
            Path = "/export.csv",
            Methods = new[] { ApiHttpMethod.GET },
            Integration = integration
        });

        new CfnOutput(this, "JournalApiUrl", new CfnOutputProps { Value = httpApi.Url! });
        new CfnOutput(this, "ImagesBucketName", new CfnOutputProps { Value = imagesBucket.BucketName });
    }
}
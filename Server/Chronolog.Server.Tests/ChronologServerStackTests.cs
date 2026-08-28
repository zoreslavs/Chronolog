using Chronolog.Server.Infrastructure;
using Amazon.CDK.Assertions;
using Amazon.CDK;
using Xunit;

namespace Chronolog.Server.Tests;

public sealed class ChronologServerStackTests
{
    [Fact]
    public void CreatesOnDemandRecordsTableAndPrivateImagesBucket()
    {
        var app = new App();
        var stack = new ChronologServerStack(app, "TestStack", GetApiCodePath());
        var template = Template.FromStack(stack);

        template.HasResourceProperties("AWS::DynamoDB::Table", new Dictionary<string, object>
        {
            ["BillingMode"] = "PAY_PER_REQUEST"
        });
        template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
        {
            ["PublicAccessBlockConfiguration"] = new Dictionary<string, object>
            {
                ["BlockPublicAcls"] = true,
                ["BlockPublicPolicy"] = true,
                ["IgnorePublicAcls"] = true,
                ["RestrictPublicBuckets"] = true
            }
        });
    }

    [Fact]
    public void CreatesAnImageDownloadRoute()
    {
        var app = new App();
        var stack = new ChronologServerStack(app, "TestStack", GetApiCodePath());
        var template = Template.FromStack(stack);

        template.HasResourceProperties("AWS::ApiGatewayV2::Route", new Dictionary<string, object>
        {
            ["RouteKey"] = "GET /records/{id}/image"
        });
    }

    private static string GetApiCodePath()
    {
        return Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "Chronolog.Server.Api",
            "bin",
            "Debug",
            "net8.0"));
    }
}

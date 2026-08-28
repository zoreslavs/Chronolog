using Amazon.Lambda.APIGatewayEvents;
using Chronolog.Server.Api;
using Chronolog.Server.Core;
using Xunit;

namespace Chronolog.Server.Tests;

public sealed class JournalApiFunctionTests
{
    [Fact]
    public async Task FunctionHandler_MapsApiGatewayRequestToJournalApi()
    {
        var function = new JournalApiFunction(new JournalApi(new EmptyJournalRecordStore(), new TestImageUploader()));

        var response = await function.FunctionHandler(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "GET /records",
            Headers = new Dictionary<string, string> { ["x-chronolog-device-id"] = "android-a1b2c3d4e5f60708" }
        }, null!);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("[]", response.Body);
    }

    private sealed class EmptyJournalRecordStore : IJournalRecordStore
    {
        public Task SaveAsync(RemoteJournalRecord record)
        {
            return Task.CompletedTask;
        }

        public Task<RemoteJournalRecord?> GetAsync(string deviceId, string recordId)
        {
            return Task.FromResult<RemoteJournalRecord?>(null);
        }

        public Task<IReadOnlyList<RemoteJournalRecord>> ListAsync(string deviceId)
        {
            return Task.FromResult<IReadOnlyList<RemoteJournalRecord>>(Array.Empty<RemoteJournalRecord>());
        }

        public Task DeleteAsync(string deviceId, string recordId)
        {
            return Task.CompletedTask;
        }
    }
}

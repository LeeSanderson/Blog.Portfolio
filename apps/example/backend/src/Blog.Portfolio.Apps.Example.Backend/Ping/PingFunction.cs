using System.Net;
using Blog.Portfolio.Shared.Backend;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Blog.Portfolio.Apps.Example.Backend.Ping;

public sealed class PingFunction : Endpoint<PingRequest, PingResponse>
{
    [Function("ExamplePing")]
    public async Task<HttpResponseData> PingAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "example/ping")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var result = await HandleAsync(new PingRequest(), cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result, cancellationToken);
        return response;
    }

    public override Task<PingResponse> HandleAsync(PingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PingResponse("pong"));
    }
}

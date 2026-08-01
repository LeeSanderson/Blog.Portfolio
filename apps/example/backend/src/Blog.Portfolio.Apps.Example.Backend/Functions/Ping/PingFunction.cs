using Blog.Portfolio.Shared.Backend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Apps.Example.Backend.Functions.Ping;

public sealed class PingFunction : Endpoint<PingRequest, PingResponse>
{
    [Function("ExamplePing")]
    public async Task<Ok<PingResponse>> PingAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "example/ping")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await HandleAsync(new PingRequest(), cancellationToken));
    }

    public override Task<PingResponse> HandleAsync(PingRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PingResponse("pong"));
}

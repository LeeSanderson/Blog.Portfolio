using Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Tokens;
using Blog.Portfolio.Shared.Backend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Unsubscribe;

public sealed class UnsubscribeFunction : Endpoint<UnsubscribeRequest, UnsubscribeResponse>
{
    private readonly SubscriberLinkAction _linkAction;

    public UnsubscribeFunction(SubscriberLinkAction linkAction) => _linkAction = linkAction;

    [Function("EmailSubscriptionUnsubscribe")]
    public async Task<Results<Ok<UnsubscribeResponse>, BadRequest<UnsubscribeResponse>>> UnsubscribeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "email-subscription/unsubscribe")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(req.Query["id"], out var subscriberId))
        {
            return TypedResults.BadRequest(new UnsubscribeResponse(false));
        }

        var response = await HandleAsync(
            new UnsubscribeRequest(subscriberId, req.Query["sig"].ToString()), cancellationToken);

        return response.Success ? TypedResults.Ok(response) : TypedResults.BadRequest(response);
    }

    public override async Task<UnsubscribeResponse> HandleAsync(UnsubscribeRequest request, CancellationToken cancellationToken)
    {
        var success = await _linkAction.TryApplyAsync(
            request.SubscriberId, request.Signature, TokenPurpose.Unsubscribe, SubscriberStatus.Unsubscribed, cancellationToken);
        return new UnsubscribeResponse(success);
    }
}

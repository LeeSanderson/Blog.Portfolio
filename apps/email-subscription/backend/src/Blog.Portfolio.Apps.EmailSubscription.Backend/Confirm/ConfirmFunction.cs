using Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Tokens;
using Blog.Portfolio.Shared.Backend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Confirm;

public sealed class ConfirmFunction : Endpoint<ConfirmRequest, ConfirmResponse>
{
    private readonly SubscriberLinkAction _linkAction;

    public ConfirmFunction(SubscriberLinkAction linkAction) => _linkAction = linkAction;

    [Function("EmailSubscriptionConfirm")]
    public async Task<Results<Ok<ConfirmResponse>, BadRequest<ConfirmResponse>>> ConfirmAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "email-subscription/confirm")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(req.Query["id"], out var subscriberId))
        {
            return TypedResults.BadRequest(new ConfirmResponse(false));
        }

        var response = await HandleAsync(
            new ConfirmRequest(subscriberId, req.Query["sig"].ToString()), cancellationToken);

        return response.Success ? TypedResults.Ok(response) : TypedResults.BadRequest(response);
    }

    public override async Task<ConfirmResponse> HandleAsync(ConfirmRequest request, CancellationToken cancellationToken)
    {
        var success = await _linkAction.TryApplyAsync(
            request.SubscriberId, request.Signature, TokenPurpose.Confirm, SubscriberStatus.Active, cancellationToken);
        return new ConfirmResponse(success);
    }
}

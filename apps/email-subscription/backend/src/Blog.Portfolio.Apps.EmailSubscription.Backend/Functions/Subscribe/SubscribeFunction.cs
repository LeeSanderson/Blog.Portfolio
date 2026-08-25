using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Shared.Backend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Subscribe;

public sealed class SubscribeFunction : Endpoint<SubscribeRequest, SubscribeResponse>
{
    private const string GenericMessage =
        "If that address isn't already subscribed, check your inbox for a confirmation email.";

    private readonly ISubscriberStore _subscriberStore;
    private readonly IEmailOutbox _emailOutbox;
    private readonly ConfirmationEmailBuilder _confirmationEmailBuilder;

    public SubscribeFunction(
        ISubscriberStore subscriberStore,
        IEmailOutbox emailOutbox,
        ConfirmationEmailBuilder confirmationEmailBuilder)
    {
        _subscriberStore = subscriberStore;
        _emailOutbox = emailOutbox;
        _confirmationEmailBuilder = confirmationEmailBuilder;
    }

    [Function("EmailSubscriptionSubscribe")]
    public async Task<Ok<SubscribeResponse>> SubscribeAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "email-subscription/subscribe")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var request = await req.ReadFromJsonAsync<SubscribeRequest>(cancellationToken)
            ?? new SubscribeRequest(string.Empty);
        return TypedResults.Ok(await HandleAsync(request, cancellationToken));
    }

    public override async Task<SubscribeResponse> HandleAsync(SubscribeRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Website))
        {
            return new SubscribeResponse(GenericMessage);
        }

        var existing = await _subscriberStore.FindByEmailAsync(request.Email, cancellationToken);

        if (existing is null)
        {
            var subscriber = new Subscriber(Guid.NewGuid(), request.Email, SubscriberStatus.Pending);
            await _subscriberStore.UpsertAsync(subscriber, cancellationToken);
            await SendConfirmationEmailAsync(subscriber, cancellationToken);
        }
        else if (existing.Status == SubscriberStatus.Unsubscribed)
        {
            var reopened = existing with { Status = SubscriberStatus.Pending };
            await _subscriberStore.UpsertAsync(reopened, cancellationToken);
            await SendConfirmationEmailAsync(reopened, cancellationToken);
        }
        else if (existing.Status == SubscriberStatus.Pending)
        {
            await SendConfirmationEmailAsync(existing, cancellationToken);
        }
        else
        {
            // Already Active: a complete no-op, so a resubmission can't be used to probe subscription status.
        }

        return new SubscribeResponse(GenericMessage);
    }

    private Task SendConfirmationEmailAsync(Subscriber subscriber, CancellationToken cancellationToken) =>
        _emailOutbox.EnqueueAsync(_confirmationEmailBuilder.Build(subscriber), cancellationToken);
}

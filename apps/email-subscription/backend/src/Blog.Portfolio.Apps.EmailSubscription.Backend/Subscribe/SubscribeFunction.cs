using Blog.Portfolio.Apps.EmailSubscription.Backend.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Tokens;
using Blog.Portfolio.Shared.Backend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribe;

public sealed class SubscribeFunction : Endpoint<SubscribeRequest, SubscribeResponse>
{
    private const string GenericMessage =
        "If that address isn't already subscribed, check your inbox for a confirmation email.";

    private readonly ISubscriberStore _subscriberStore;
    private readonly IEmailOutbox _emailOutbox;
    private readonly SubscriberLinkBuilder _linkBuilder;
    private readonly EmailSubscriptionOptions _options;

    public SubscribeFunction(
        ISubscriberStore subscriberStore,
        IEmailOutbox emailOutbox,
        SubscriberLinkBuilder linkBuilder,
        IOptions<EmailSubscriptionOptions> options)
    {
        _subscriberStore = subscriberStore;
        _emailOutbox = emailOutbox;
        _linkBuilder = linkBuilder;
        _options = options.Value;
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

    private Task SendConfirmationEmailAsync(Subscriber subscriber, CancellationToken cancellationToken)
    {
        var confirmLink = _linkBuilder.Build(_options.ConfirmPageUrl, subscriber.Id, TokenPurpose.Confirm);
        var unsubscribeLink = _linkBuilder.Build(_options.UnsubscribePageUrl, subscriber.Id, TokenPurpose.Unsubscribe);

        var html = $"""
            <p>Thanks for subscribing to sixsideddice.com blog updates!</p>
            <p><a href="{confirmLink}">Confirm your subscription</a></p>
            <p>Didn't request this? <a href="{unsubscribeLink}">Unsubscribe</a>.</p>
            """;

        return _emailOutbox.EnqueueAsync(
            new SendEmailMessage(subscriber.Email, "Confirm your subscription", html), cancellationToken);
    }
}

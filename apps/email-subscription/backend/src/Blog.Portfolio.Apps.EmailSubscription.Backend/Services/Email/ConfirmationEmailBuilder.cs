using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public sealed class ConfirmationEmailBuilder
{
    private readonly SubscriberLinkBuilder _linkBuilder;
    private readonly EmailSubscriptionOptions _options;

    public ConfirmationEmailBuilder(SubscriberLinkBuilder linkBuilder, IOptions<EmailSubscriptionOptions> options)
    {
        _linkBuilder = linkBuilder;
        _options = options.Value;
    }

    public SendEmailMessage Build(Subscriber subscriber)
    {
        var confirmLink = _linkBuilder.Build(_options.ConfirmPageUrl, subscriber.Id, TokenPurpose.Confirm);
        var unsubscribeLink = _linkBuilder.Build(_options.UnsubscribePageUrl, subscriber.Id, TokenPurpose.Unsubscribe);

        var html = $"""
            <p>Thanks for subscribing to sixsideddice.com blog updates!</p>
            <p><a href="{confirmLink}">Confirm your subscription</a></p>
            <p>Didn't request this? <a href="{unsubscribeLink}">Unsubscribe</a>.</p>
            """;

        return new SendEmailMessage(subscriber.Email, "Confirm your subscription", html);
    }
}

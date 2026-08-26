using AwesomeAssertions;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Services.Email;

public class ConfirmationEmailBuilderTests
{
    private static readonly Subscriber PendingSubscriber = new(
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), "reader@example.com", SubscriberStatus.Pending);

    private readonly ConfirmationEmailBuilder _builder = new(
        new SubscriberLinkBuilder(new HmacSubscriberTokenService("test-signing-key")),
        Options.Create(new EmailSubscriptionOptions()));

    [Fact]
    public async Task Build_RendersTheConfirmationEmail()
    {
        var message = _builder.Build(PendingSubscriber);

        message.To.Should().Be("reader@example.com");
        message.Subject.Should().Be("Confirm your subscription");
        await Verify(message.HtmlBody, "html");
    }
}

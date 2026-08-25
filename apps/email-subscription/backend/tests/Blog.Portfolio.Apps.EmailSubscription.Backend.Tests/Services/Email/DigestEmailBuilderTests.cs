using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Services.Email;

public class DigestEmailBuilderTests
{
    private static readonly DateTimeOffset PublishedAt = new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero);

    private static readonly Subscriber ActiveSubscriber = new(
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), "reader@example.com", SubscriberStatus.Active);

    private static readonly BlogPost[] Posts =
    [
        new BlogPost(
            "Rolling for Initiative",
            "https://www.sixsideddice.com/posts/rolling-for-initiative",
            "How turn order shapes the opening minutes of a session.",
            PublishedAt),
        new BlogPost(
            "A Dozen Dice Drills",
            "https://www.sixsideddice.com/posts/a-dozen-dice-drills",
            "Twelve short exercises for faster table maths.",
            PublishedAt.AddDays(-3)),
    ];

    private readonly DigestEmailBuilder _builder = new(
        new SubscriberLinkBuilder(new HmacSubscriberTokenService("test-signing-key")),
        Options.Create(new EmailSubscriptionOptions()));

    [Fact]
    public async Task Build_RendersOneListEntryPerPostInTheOrderGiven()
    {
        var message = _builder.Build(ActiveSubscriber, Posts);

        message.To.Should().Be("reader@example.com");
        message.Subject.Should().Be("New posts on sixsideddice.com");
        await Verify(message.HtmlBody, "html");
    }
}

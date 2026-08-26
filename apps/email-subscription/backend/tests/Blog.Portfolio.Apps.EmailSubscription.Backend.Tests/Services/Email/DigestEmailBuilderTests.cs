using AwesomeAssertions;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
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

    [Fact]
    public void Build_HtmlEncodesEveryFieldItTakesFromTheFeed()
    {
        var awkwardPost = new BlogPost(
            "Dice & Dragons: <b>a primer</b>",
            "https://www.sixsideddice.com/posts/dice-and-dragons?tag=d&d",
            "Ampersands & angle brackets < > in a teaser.",
            PublishedAt);

        var message = _builder.Build(ActiveSubscriber, [awkwardPost]);

        message.HtmlBody.Should().Contain(
            """<li><a href="https://www.sixsideddice.com/posts/dice-and-dragons?tag=d&amp;d">Dice &amp; Dragons: &lt;b&gt;a primer&lt;/b&gt;</a><p>Ampersands &amp; angle brackets &lt; &gt; in a teaser.</p></li>""");
    }
}

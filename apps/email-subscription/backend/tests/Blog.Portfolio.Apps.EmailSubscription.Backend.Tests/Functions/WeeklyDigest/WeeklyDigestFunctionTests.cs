using Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.WeeklyDigest;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Functions.WeeklyDigest;

public class WeeklyDigestFunctionTests
{
    private static readonly DateTimeOffset RunTime = new(2026, 7, 27, 8, 0, 0, TimeSpan.Zero);

    private readonly IBlogFeedReader _feedReader = Substitute.For<IBlogFeedReader>();
    private readonly ISubscriberStore _subscriberStore = Substitute.For<ISubscriberStore>();
    private readonly IEmailOutbox _emailOutbox = Substitute.For<IEmailOutbox>();
    private readonly WeeklyDigestFunction _function;

    public WeeklyDigestFunctionTests()
    {
        _function = new WeeklyDigestFunction(
            _feedReader,
            _subscriberStore,
            _emailOutbox,
            new DigestEmailBuilder(
                new SubscriberLinkBuilder(new HmacSubscriberTokenService("test-signing-key")),
                Options.Create(new EmailSubscriptionOptions())));
    }

    [Fact]
    public async Task HandleAsync_WithAPostPublishedWithinTheLastSevenDays_EmailsEveryActiveSubscriber()
    {
        var recentPost = new BlogPost("New post", "https://www.sixsideddice.com/post", "Teaser", RunTime.AddDays(-3));
        _feedReader.GetPostsAsync(Arg.Any<CancellationToken>()).Returns([recentPost]);
        var subscribers = new[]
        {
            new Subscriber(Guid.NewGuid(), "one@example.com", SubscriberStatus.Active),
            new Subscriber(Guid.NewGuid(), "two@example.com", SubscriberStatus.Active),
        };
        _subscriberStore.ListByStatusAsync(SubscriberStatus.Active, Arg.Any<CancellationToken>())
            .Returns(subscribers);

        await _function.HandleAsync(RunTime, CancellationToken.None);

        await _emailOutbox.Received(1).EnqueueAsync(
            Arg.Is<SendEmailMessage>(m => m.To == "one@example.com"), Arg.Any<CancellationToken>());
        await _emailOutbox.Received(1).EnqueueAsync(
            Arg.Is<SendEmailMessage>(m => m.To == "two@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithAPostOlderThanSevenDays_ExcludesItAndSendsNothing()
    {
        var oldPost = new BlogPost("Old post", "https://www.sixsideddice.com/old", "Teaser", RunTime.AddDays(-8));
        _feedReader.GetPostsAsync(Arg.Any<CancellationToken>()).Returns([oldPost]);

        await _function.HandleAsync(RunTime, CancellationToken.None);

        await _subscriberStore.DidNotReceive()
            .ListByStatusAsync(Arg.Any<SubscriberStatus>(), Arg.Any<CancellationToken>());
        await _emailOutbox.DidNotReceive()
            .EnqueueAsync(Arg.Any<SendEmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNoPostsAtAll_SendsNothing()
    {
        _feedReader.GetPostsAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _function.HandleAsync(RunTime, CancellationToken.None);

        await _subscriberStore.DidNotReceive()
            .ListByStatusAsync(Arg.Any<SubscriberStatus>(), Arg.Any<CancellationToken>());
        await _emailOutbox.DidNotReceive()
            .EnqueueAsync(Arg.Any<SendEmailMessage>(), Arg.Any<CancellationToken>());
    }
}

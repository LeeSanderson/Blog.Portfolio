using Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.WeeklyDigest;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Options;
using Moq;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Functions.WeeklyDigest;

public class WeeklyDigestFunctionTests
{
    private static readonly DateTimeOffset RunTime = new(2026, 7, 27, 8, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBlogFeedReader> _feedReader = new();
    private readonly Mock<ISubscriberStore> _subscriberStore = new();
    private readonly Mock<IEmailOutbox> _emailOutbox = new();
    private readonly WeeklyDigestFunction _function;

    public WeeklyDigestFunctionTests()
    {
        _function = new WeeklyDigestFunction(
            _feedReader.Object,
            _subscriberStore.Object,
            _emailOutbox.Object,
            new DigestEmailBuilder(
                new SubscriberLinkBuilder(new HmacSubscriberTokenService("test-signing-key")),
                Options.Create(new EmailSubscriptionOptions())));
    }

    [Fact]
    public async Task HandleAsync_WithAPostPublishedWithinTheLastSevenDays_EmailsEveryActiveSubscriber()
    {
        var recentPost = new BlogPost("New post", "https://www.sixsideddice.com/post", "Teaser", RunTime.AddDays(-3));
        _feedReader.Setup(reader => reader.GetPostsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([recentPost]);
        var subscribers = new[]
        {
            new Subscriber(Guid.NewGuid(), "one@example.com", SubscriberStatus.Active),
            new Subscriber(Guid.NewGuid(), "two@example.com", SubscriberStatus.Active),
        };
        _subscriberStore.Setup(store => store.ListByStatusAsync(SubscriberStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscribers);

        await _function.HandleAsync(RunTime, CancellationToken.None);

        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.Is<SendEmailMessage>(m => m.To == "one@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.Is<SendEmailMessage>(m => m.To == "two@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithAPostOlderThanSevenDays_ExcludesItAndSendsNothing()
    {
        var oldPost = new BlogPost("Old post", "https://www.sixsideddice.com/old", "Teaser", RunTime.AddDays(-8));
        _feedReader.Setup(reader => reader.GetPostsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([oldPost]);

        await _function.HandleAsync(RunTime, CancellationToken.None);

        _subscriberStore.Verify(
            store => store.ListByStatusAsync(It.IsAny<SubscriberStatus>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailOutbox.Verify(
            outbox => outbox.EnqueueAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNoPostsAtAll_SendsNothing()
    {
        _feedReader.Setup(reader => reader.GetPostsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _function.HandleAsync(RunTime, CancellationToken.None);

        _subscriberStore.Verify(
            store => store.ListByStatusAsync(It.IsAny<SubscriberStatus>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailOutbox.Verify(
            outbox => outbox.EnqueueAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

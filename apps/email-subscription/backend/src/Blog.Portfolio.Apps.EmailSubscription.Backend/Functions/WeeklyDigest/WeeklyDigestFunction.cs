using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.WeeklyDigest;

public sealed class WeeklyDigestFunction
{
    private static readonly TimeSpan WindowLength = TimeSpan.FromDays(7);

    private readonly IBlogFeedReader _feedReader;
    private readonly ISubscriberStore _subscriberStore;
    private readonly IEmailOutbox _emailOutbox;
    private readonly DigestEmailBuilder _digestEmailBuilder;

    public WeeklyDigestFunction(
        IBlogFeedReader feedReader,
        ISubscriberStore subscriberStore,
        IEmailOutbox emailOutbox,
        DigestEmailBuilder digestEmailBuilder)
    {
        _feedReader = feedReader;
        _subscriberStore = subscriberStore;
        _emailOutbox = emailOutbox;
        _digestEmailBuilder = digestEmailBuilder;
    }

    // No try/catch here: an unhandled RSS fetch/parse failure is caught and logged to Application Insights by
    // the Functions host itself, and this timer has no configured retry policy, satisfying ticket 03's "logged,
    // not retried" requirement without duplicating that behavior in application code.
    [Function("EmailSubscriptionWeeklyDigest")]
    public Task RunAsync([TimerTrigger("0 0 8 * * 1")] TimerInfo timer, CancellationToken cancellationToken) =>
        HandleAsync(TimeProvider.System.GetUtcNow(), cancellationToken);

    public async Task HandleAsync(DateTimeOffset runTimeUtc, CancellationToken cancellationToken)
    {
        var posts = await _feedReader.GetPostsAsync(cancellationToken);

        var windowStart = runTimeUtc - WindowLength;
        var recentPosts = posts.Where(post => post.PublishedAtUtc >= windowStart && post.PublishedAtUtc <= runTimeUtc).ToList();

        if (recentPosts.Count == 0)
        {
            return;
        }

        var activeSubscribers = await _subscriberStore.ListByStatusAsync(SubscriberStatus.Active, cancellationToken);

        foreach (var subscriber in activeSubscribers)
        {
            await _emailOutbox.EnqueueAsync(_digestEmailBuilder.Build(subscriber, recentPosts), cancellationToken);
        }
    }
}

using Blog.Portfolio.Apps.EmailSubscription.Backend.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Tokens;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.WeeklyDigest;

public sealed class WeeklyDigestFunction
{
    private static readonly TimeSpan WindowLength = TimeSpan.FromDays(7);

    private readonly IBlogFeedReader _feedReader;
    private readonly ISubscriberStore _subscriberStore;
    private readonly IEmailOutbox _emailOutbox;
    private readonly SubscriberLinkBuilder _linkBuilder;
    private readonly EmailSubscriptionOptions _options;

    public WeeklyDigestFunction(
        IBlogFeedReader feedReader,
        ISubscriberStore subscriberStore,
        IEmailOutbox emailOutbox,
        SubscriberLinkBuilder linkBuilder,
        IOptions<EmailSubscriptionOptions> options)
    {
        _feedReader = feedReader;
        _subscriberStore = subscriberStore;
        _emailOutbox = emailOutbox;
        _linkBuilder = linkBuilder;
        _options = options.Value;
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
            await _emailOutbox.EnqueueAsync(BuildDigestMessage(subscriber, recentPosts), cancellationToken);
        }
    }

    private SendEmailMessage BuildDigestMessage(Subscriber subscriber, IEnumerable<BlogPost> posts)
    {
        var unsubscribeLink = _linkBuilder.Build(_options.UnsubscribePageUrl, subscriber.Id, TokenPurpose.Unsubscribe);

        var postsHtml = string.Concat(posts.Select(post =>
            $"""<li><a href="{post.Link}">{post.Title}</a><p>{post.Description}</p></li>"""));

        var html = $"""
            <p>New posts on sixsideddice.com this week:</p>
            <ul>{postsHtml}</ul>
            <p><a href="{unsubscribeLink}">Unsubscribe</a></p>
            """;

        return new SendEmailMessage(subscriber.Email, "New posts on sixsideddice.com", html);
    }
}

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;

public interface IBlogFeedReader
{
    Task<IReadOnlyList<BlogPost>> GetPostsAsync(CancellationToken cancellationToken);
}

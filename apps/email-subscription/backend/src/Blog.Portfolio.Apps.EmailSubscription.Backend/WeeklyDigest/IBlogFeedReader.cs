namespace Blog.Portfolio.Apps.EmailSubscription.Backend.WeeklyDigest;

public interface IBlogFeedReader
{
    Task<IReadOnlyList<BlogPost>> GetPostsAsync(CancellationToken cancellationToken);
}

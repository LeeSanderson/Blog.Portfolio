namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;

public sealed record BlogPost(string Title, string Link, string Description, DateTimeOffset PublishedAtUtc);

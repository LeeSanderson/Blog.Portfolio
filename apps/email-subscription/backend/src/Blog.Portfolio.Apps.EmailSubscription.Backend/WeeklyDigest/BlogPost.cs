namespace Blog.Portfolio.Apps.EmailSubscription.Backend.WeeklyDigest;

public sealed record BlogPost(string Title, string Link, string Description, DateTimeOffset PublishedAtUtc);

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;

public sealed record Subscriber(Guid Id, string Email, SubscriberStatus Status);

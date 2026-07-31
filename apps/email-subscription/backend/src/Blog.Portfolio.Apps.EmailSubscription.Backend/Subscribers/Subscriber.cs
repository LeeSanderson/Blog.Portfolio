namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;

public sealed record Subscriber(Guid Id, string Email, SubscriberStatus Status);

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribe;

public sealed record SubscribeRequest(string Email, string? Website = null);

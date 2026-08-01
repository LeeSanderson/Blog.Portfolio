namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Subscribe;

public sealed record SubscribeRequest(string Email, string? Website = null);

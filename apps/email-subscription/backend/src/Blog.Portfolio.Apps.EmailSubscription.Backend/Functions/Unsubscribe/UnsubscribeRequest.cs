namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Unsubscribe;

public sealed record UnsubscribeRequest(Guid SubscriberId, string Signature);

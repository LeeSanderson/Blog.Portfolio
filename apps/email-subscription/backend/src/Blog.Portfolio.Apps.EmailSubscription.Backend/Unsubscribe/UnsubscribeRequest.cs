namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Unsubscribe;

public sealed record UnsubscribeRequest(Guid SubscriberId, string Signature);

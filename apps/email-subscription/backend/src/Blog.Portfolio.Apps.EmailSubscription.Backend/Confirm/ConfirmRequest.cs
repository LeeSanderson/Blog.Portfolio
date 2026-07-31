namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Confirm;

public sealed record ConfirmRequest(Guid SubscriberId, string Signature);

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Confirm;

public sealed record ConfirmRequest(Guid SubscriberId, string Signature);

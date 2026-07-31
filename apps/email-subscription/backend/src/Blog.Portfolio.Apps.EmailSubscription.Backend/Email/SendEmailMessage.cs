namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Email;

public sealed record SendEmailMessage(string To, string Subject, string HtmlBody)
{
#pragma warning disable S2339 // Must be a compile-time constant: consumed by the [QueueTrigger] attribute
    public const string QueueName = "send-email";
#pragma warning restore S2339
}

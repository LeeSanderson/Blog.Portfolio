namespace Blog.Portfolio.Apps.EmailSubscription.Backend;

public sealed class EmailSubscriptionOptions
{
    public string ResendApiKey { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public Uri ConfirmPageUrl { get; set; } = new("https://www.sixsideddice.com/subscribe/confirm");

    public Uri UnsubscribePageUrl { get; set; } = new("https://www.sixsideddice.com/subscribe/unsubscribe");
}

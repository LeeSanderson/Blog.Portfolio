namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;

public sealed class SubscriberLinkBuilder
{
    private readonly ISubscriberTokenService _tokenService;

    public SubscriberLinkBuilder(ISubscriberTokenService tokenService) => _tokenService = tokenService;

    public string Build(Uri pageUrl, Guid subscriberId, TokenPurpose purpose)
    {
        var signature = _tokenService.CreateSignature(subscriberId, purpose);
        return $"{pageUrl}?id={subscriberId:N}&sig={signature}";
    }
}

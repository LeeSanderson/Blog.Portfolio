using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;

public sealed class SubscriberLinkAction
{
    private readonly ISubscriberStore _subscriberStore;
    private readonly ISubscriberTokenService _tokenService;

    public SubscriberLinkAction(ISubscriberStore subscriberStore, ISubscriberTokenService tokenService)
    {
        _subscriberStore = subscriberStore;
        _tokenService = tokenService;
    }

    public async Task<bool> TryApplyAsync(
        Guid subscriberId, string signature, TokenPurpose purpose, SubscriberStatus newStatus, CancellationToken cancellationToken)
    {
        if (!_tokenService.IsValid(subscriberId, purpose, signature))
        {
            return false;
        }

        var subscriber = await _subscriberStore.FindByIdAsync(subscriberId, cancellationToken);
        if (subscriber is null)
        {
            return false;
        }

        await _subscriberStore.UpsertAsync(subscriber with { Status = newStatus }, cancellationToken);
        return true;
    }
}

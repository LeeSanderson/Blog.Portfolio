namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tokens;

public interface ISubscriberTokenService
{
    string CreateSignature(Guid subscriberId, TokenPurpose purpose);

    bool IsValid(Guid subscriberId, TokenPurpose purpose, string signature);
}

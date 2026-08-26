using AwesomeAssertions;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Services.Tokens;

public class HmacSubscriberTokenServiceTests
{
    private static readonly Guid SubscriberId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly HmacSubscriberTokenService _tokenService = new("correct-horse-battery-staple");

    [Fact]
    public void IsValid_ReturnsTrueForASignatureItCreatedForTheSamePurpose()
    {
        var signature = _tokenService.CreateSignature(SubscriberId, TokenPurpose.Confirm);

        _tokenService.IsValid(SubscriberId, TokenPurpose.Confirm, signature).Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenTheSignatureIsForADifferentSubscriber()
    {
        var otherSubscriberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var signature = _tokenService.CreateSignature(SubscriberId, TokenPurpose.Confirm);

        _tokenService.IsValid(otherSubscriberId, TokenPurpose.Confirm, signature).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenAConfirmSignatureIsPresentedForUnsubscribe()
    {
        var confirmSignature = _tokenService.CreateSignature(SubscriberId, TokenPurpose.Confirm);

        _tokenService.IsValid(SubscriberId, TokenPurpose.Unsubscribe, confirmSignature).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalseForAnArbitraryMalformedSignature()
    {
        _tokenService.IsValid(SubscriberId, TokenPurpose.Confirm, "not-a-real-signature").Should().BeFalse();
    }
}

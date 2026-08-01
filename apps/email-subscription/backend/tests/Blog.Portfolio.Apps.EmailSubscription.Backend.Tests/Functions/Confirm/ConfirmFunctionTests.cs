using Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Confirm;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using FluentAssertions;
using Moq;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Functions.Confirm;

public class ConfirmFunctionTests
{
    private static readonly Guid SubscriberId = Guid.NewGuid();

    private readonly Mock<ISubscriberStore> _subscriberStore = new();
    private readonly HmacSubscriberTokenService _tokenService = new("test-signing-key");
    private readonly ConfirmFunction _function;

    public ConfirmFunctionTests()
    {
        _function = new ConfirmFunction(new SubscriberLinkAction(_subscriberStore.Object, _tokenService));
    }

    [Fact]
    public async Task HandleAsync_WithAValidSignature_SetsTheSubscriberToActive()
    {
        var subscriber = new Subscriber(SubscriberId, "reader@example.com", SubscriberStatus.Pending);
        _subscriberStore.Setup(store => store.FindByIdAsync(SubscriberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriber);
        var signature = _tokenService.CreateSignature(SubscriberId, TokenPurpose.Confirm);

        var response = await _function.HandleAsync(new ConfirmRequest(SubscriberId, signature), CancellationToken.None);

        response.Success.Should().BeTrue();
        _subscriberStore.Verify(store => store.UpsertAsync(
            It.Is<Subscriber>(s => s.Id == SubscriberId && s.Status == SubscriberStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ConfirmingAnAlreadyUnsubscribedSubscriber_MovesItBackToActive()
    {
        var subscriber = new Subscriber(SubscriberId, "reader@example.com", SubscriberStatus.Unsubscribed);
        _subscriberStore.Setup(store => store.FindByIdAsync(SubscriberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriber);
        var signature = _tokenService.CreateSignature(SubscriberId, TokenPurpose.Confirm);

        var response = await _function.HandleAsync(new ConfirmRequest(SubscriberId, signature), CancellationToken.None);

        response.Success.Should().BeTrue();
        _subscriberStore.Verify(store => store.UpsertAsync(
            It.Is<Subscriber>(s => s.Status == SubscriberStatus.Active), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithAnInvalidSignature_DoesNotChangeTheSubscriber()
    {
        var response = await _function.HandleAsync(
            new ConfirmRequest(SubscriberId, "not-a-real-signature"), CancellationToken.None);

        response.Success.Should().BeFalse();
        _subscriberStore.Verify(store => store.UpsertAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithAnUnsubscribeSignaturePresentedToConfirm_IsRejected()
    {
        var unsubscribeSignature = _tokenService.CreateSignature(SubscriberId, TokenPurpose.Unsubscribe);

        var response = await _function.HandleAsync(
            new ConfirmRequest(SubscriberId, unsubscribeSignature), CancellationToken.None);

        response.Success.Should().BeFalse();
        _subscriberStore.Verify(store => store.UpsertAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

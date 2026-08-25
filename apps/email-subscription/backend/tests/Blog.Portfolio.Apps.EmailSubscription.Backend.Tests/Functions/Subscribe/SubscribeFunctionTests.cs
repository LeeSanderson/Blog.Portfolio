using Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Subscribe;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Functions.Subscribe;

public class SubscribeFunctionTests
{
    private const string Email = "reader@example.com";

    private readonly Mock<ISubscriberStore> _subscriberStore = new();
    private readonly Mock<IEmailOutbox> _emailOutbox = new();
    private readonly SubscribeFunction _function;

    public SubscribeFunctionTests()
    {
        _function = new SubscribeFunction(
            _subscriberStore.Object,
            _emailOutbox.Object,
            new ConfirmationEmailBuilder(
                new SubscriberLinkBuilder(new HmacSubscriberTokenService("test-signing-key")),
                Options.Create(new EmailSubscriptionOptions())));
    }

    [Fact]
    public async Task HandleAsync_ForANewEmail_CreatesAPendingSubscriberAndSendsAConfirmationEmail()
    {
        _subscriberStore.Setup(store => store.FindByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Subscriber));

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        _subscriberStore.Verify(store => store.UpsertAsync(
            It.Is<Subscriber>(s => s.Email == Email && s.Status == SubscriberStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.Is<SendEmailMessage>(m => m.To == Email), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ForAnUnsubscribedEmail_ReopensItToPendingAndSendsAConfirmationEmail()
    {
        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Unsubscribed);
        _subscriberStore.Setup(store => store.FindByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        _subscriberStore.Verify(store => store.UpsertAsync(
            It.Is<Subscriber>(s => s.Id == existing.Id && s.Status == SubscriberStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ForAPendingEmail_ResendsTheConfirmationEmailWithoutChangingTheRecord()
    {
        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Pending);
        _subscriberStore.Setup(store => store.FindByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        _subscriberStore.Verify(store => store.UpsertAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ForAnActiveEmail_IsACompleteNoOp()
    {
        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Active);
        _subscriberStore.Setup(store => store.FindByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        _subscriberStore.Verify(store => store.UpsertAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTheHoneypotFieldIsFilled_SilentlyNoOpsWithoutTouchingAnyRecord()
    {
        await _function.HandleAsync(new SubscribeRequest(Email, Website: "https://spam.example"), CancellationToken.None);

        _subscriberStore.Verify(store => store.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriberStore.Verify(store => store.UpsertAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailOutbox.Verify(outbox => outbox.EnqueueAsync(
            It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlwaysReturnsTheSameGenericMessageRegardlessOfPriorState()
    {
        _subscriberStore.Setup(store => store.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Subscriber));
        var newEmailResponse = await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Active);
        _subscriberStore.Setup(store => store.FindByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var activeEmailResponse = await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        var honeypotResponse = await _function.HandleAsync(new SubscribeRequest(Email, Website: "spam"), CancellationToken.None);

        newEmailResponse.Message.Should().Be(activeEmailResponse.Message);
        newEmailResponse.Message.Should().Be(honeypotResponse.Message);
    }
}

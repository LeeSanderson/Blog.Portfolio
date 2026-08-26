using AwesomeAssertions;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.Subscribe;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Functions.Subscribe;

public class SubscribeFunctionTests
{
    private const string Email = "reader@example.com";

    private readonly ISubscriberStore _subscriberStore = Substitute.For<ISubscriberStore>();
    private readonly IEmailOutbox _emailOutbox = Substitute.For<IEmailOutbox>();
    private readonly SubscribeFunction _function;

    public SubscribeFunctionTests()
    {
        _function = new SubscribeFunction(
            _subscriberStore,
            _emailOutbox,
            new ConfirmationEmailBuilder(
                new SubscriberLinkBuilder(new HmacSubscriberTokenService("test-signing-key")),
                Options.Create(new EmailSubscriptionOptions())));
    }

    [Fact]
    public async Task HandleAsync_ForANewEmail_CreatesAPendingSubscriberAndSendsAConfirmationEmail()
    {
        _subscriberStore.FindByEmailAsync(Email, Arg.Any<CancellationToken>())
            .ReturnsNull();

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        await _subscriberStore.Received(1).UpsertAsync(
            Arg.Is<Subscriber>(s => s.Email == Email && s.Status == SubscriberStatus.Pending),
            Arg.Any<CancellationToken>());
        await _emailOutbox.Received(1).EnqueueAsync(
            Arg.Is<SendEmailMessage>(m => m.To == Email), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForAnUnsubscribedEmail_ReopensItToPendingAndSendsAConfirmationEmail()
    {
        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Unsubscribed);
        _subscriberStore.FindByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        await _subscriberStore.Received(1).UpsertAsync(
            Arg.Is<Subscriber>(s => s.Id == existing.Id && s.Status == SubscriberStatus.Pending),
            Arg.Any<CancellationToken>());
        await _emailOutbox.Received(1).EnqueueAsync(
            Arg.Any<SendEmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForAPendingEmail_ResendsTheConfirmationEmailWithoutChangingTheRecord()
    {
        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Pending);
        _subscriberStore.FindByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        await _subscriberStore.DidNotReceive().UpsertAsync(Arg.Any<Subscriber>(), Arg.Any<CancellationToken>());
        await _emailOutbox.Received(1).EnqueueAsync(
            Arg.Any<SendEmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForAnActiveEmail_IsACompleteNoOp()
    {
        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Active);
        _subscriberStore.FindByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        await _subscriberStore.DidNotReceive().UpsertAsync(Arg.Any<Subscriber>(), Arg.Any<CancellationToken>());
        await _emailOutbox.DidNotReceive().EnqueueAsync(
            Arg.Any<SendEmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTheHoneypotFieldIsFilled_SilentlyNoOpsWithoutTouchingAnyRecord()
    {
        await _function.HandleAsync(new SubscribeRequest(Email, Website: "https://spam.example"), CancellationToken.None);

        await _subscriberStore.DidNotReceive().FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _subscriberStore.DidNotReceive().UpsertAsync(Arg.Any<Subscriber>(), Arg.Any<CancellationToken>());
        await _emailOutbox.DidNotReceive().EnqueueAsync(
            Arg.Any<SendEmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlwaysReturnsTheSameGenericMessageRegardlessOfPriorState()
    {
        _subscriberStore.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        var newEmailResponse = await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        var existing = new Subscriber(Guid.NewGuid(), Email, SubscriberStatus.Active);
        _subscriberStore.FindByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existing);
        var activeEmailResponse = await _function.HandleAsync(new SubscribeRequest(Email), CancellationToken.None);

        var honeypotResponse = await _function.HandleAsync(new SubscribeRequest(Email, Website: "spam"), CancellationToken.None);

        newEmailResponse.Message.Should().Be(activeEmailResponse.Message);
        newEmailResponse.Message.Should().Be(honeypotResponse.Message);
    }
}

using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Email;

public sealed class SendEmailFunction
{
    private readonly IEmailSender _emailSender;

    public SendEmailFunction(IEmailSender emailSender) => _emailSender = emailSender;

    [Function("EmailSubscriptionSendEmail")]
    public Task RunAsync(
        [QueueTrigger(SendEmailMessage.QueueName)] SendEmailMessage message,
        CancellationToken cancellationToken) =>
        HandleAsync(message, cancellationToken);

    public Task HandleAsync(SendEmailMessage message, CancellationToken cancellationToken) =>
        _emailSender.SendAsync(message, cancellationToken);
}

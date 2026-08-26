using Blog.Portfolio.Apps.EmailSubscription.Backend.Functions.SendEmail;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using NSubstitute;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Functions.SendEmail;

public class SendEmailFunctionTests
{
    [Fact]
    public async Task HandleAsync_SendsTheQueuedMessageViaTheEmailSender()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var function = new SendEmailFunction(emailSender);
        var message = new SendEmailMessage("reader@example.com", "New post!", "<p>Hello</p>");

        await function.HandleAsync(message, CancellationToken.None);

        await emailSender.Received(1).SendAsync(message, CancellationToken.None);
    }
}

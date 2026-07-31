using Blog.Portfolio.Apps.EmailSubscription.Backend.Email;
using Moq;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests.Email;

public class SendEmailFunctionTests
{
    [Fact]
    public async Task HandleAsync_SendsTheQueuedMessageViaTheEmailSender()
    {
        var emailSender = new Mock<IEmailSender>();
        var function = new SendEmailFunction(emailSender.Object);
        var message = new SendEmailMessage("reader@example.com", "New post!", "<p>Hello</p>");

        await function.HandleAsync(message, CancellationToken.None);

        emailSender.Verify(sender => sender.SendAsync(message, CancellationToken.None), Times.Once);
    }
}

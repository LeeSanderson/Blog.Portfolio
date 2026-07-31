namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Email;

public interface IEmailSender
{
    Task SendAsync(SendEmailMessage message, CancellationToken cancellationToken);
}

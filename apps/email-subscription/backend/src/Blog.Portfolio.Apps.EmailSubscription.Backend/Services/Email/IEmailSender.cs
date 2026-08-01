namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public interface IEmailSender
{
    Task SendAsync(SendEmailMessage message, CancellationToken cancellationToken);
}

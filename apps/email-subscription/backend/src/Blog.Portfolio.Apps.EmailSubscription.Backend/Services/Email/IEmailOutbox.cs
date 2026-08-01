namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public interface IEmailOutbox
{
    Task EnqueueAsync(SendEmailMessage message, CancellationToken cancellationToken);
}

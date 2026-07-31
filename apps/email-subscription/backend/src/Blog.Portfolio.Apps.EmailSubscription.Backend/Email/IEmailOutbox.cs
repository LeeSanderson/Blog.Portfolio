namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Email;

public interface IEmailOutbox
{
    Task EnqueueAsync(SendEmailMessage message, CancellationToken cancellationToken);
}

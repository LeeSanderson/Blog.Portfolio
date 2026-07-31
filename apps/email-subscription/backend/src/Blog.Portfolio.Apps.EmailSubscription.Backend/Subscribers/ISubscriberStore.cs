namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;

public interface ISubscriberStore
{
    Task<Subscriber?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Subscriber?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpsertAsync(Subscriber subscriber, CancellationToken cancellationToken);

    Task<IReadOnlyList<Subscriber>> ListByStatusAsync(SubscriberStatus status, CancellationToken cancellationToken);
}

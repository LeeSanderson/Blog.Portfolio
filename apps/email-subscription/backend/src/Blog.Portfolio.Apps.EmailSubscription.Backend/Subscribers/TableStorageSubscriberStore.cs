using Azure;
using Azure.Data.Tables;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Subscribers;

public sealed class TableStorageSubscriberStore : ISubscriberStore
{
    private const int NotFoundStatusCode = 404;

    private readonly TableClient _tableClient;

    public TableStorageSubscriberStore(TableClient tableClient) => _tableClient = tableClient;

    public async Task<Subscriber?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {SubscriberTableEntity.PartitionKeyValue} and Email eq {email}");

        var results = _tableClient.QueryAsync<SubscriberTableEntity>(filter, cancellationToken: cancellationToken);
        await using var enumerator = results.GetAsyncEnumerator(cancellationToken);

        return await enumerator.MoveNextAsync() ? ToSubscriber(enumerator.Current) : null;
    }

    public async Task<Subscriber?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<SubscriberTableEntity>(
                SubscriberTableEntity.PartitionKeyValue, id.ToString(), cancellationToken: cancellationToken);
            return ToSubscriber(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == NotFoundStatusCode)
        {
            return null;
        }
    }

    public Task UpsertAsync(Subscriber subscriber, CancellationToken cancellationToken)
    {
        var entity = new SubscriberTableEntity
        {
            RowKey = subscriber.Id.ToString(),
            Email = subscriber.Email,
            Status = subscriber.Status.ToString(),
        };

        return _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<IReadOnlyList<Subscriber>> ListByStatusAsync(SubscriberStatus status, CancellationToken cancellationToken)
    {
        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {SubscriberTableEntity.PartitionKeyValue} and Status eq {status.ToString()}");

        var subscribers = new List<Subscriber>();
        await foreach (var entity in _tableClient.QueryAsync<SubscriberTableEntity>(filter, cancellationToken: cancellationToken))
        {
            subscribers.Add(ToSubscriber(entity));
        }

        return subscribers;
    }

    private static Subscriber ToSubscriber(SubscriberTableEntity entity) =>
        new(Guid.Parse(entity.RowKey), entity.Email, Enum.Parse<SubscriberStatus>(entity.Status, ignoreCase: true));
}

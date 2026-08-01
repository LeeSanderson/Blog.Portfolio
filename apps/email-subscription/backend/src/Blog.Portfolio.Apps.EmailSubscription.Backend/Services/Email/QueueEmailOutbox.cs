using System.Text.Json;
using Azure.Storage.Queues;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public sealed class QueueEmailOutbox : IEmailOutbox
{
    private readonly QueueClient _queueClient;

    public QueueEmailOutbox(QueueClient queueClient) => _queueClient = queueClient;

    public Task EnqueueAsync(SendEmailMessage message, CancellationToken cancellationToken) =>
        _queueClient.SendMessageAsync(JsonSerializer.Serialize(message), cancellationToken);
}

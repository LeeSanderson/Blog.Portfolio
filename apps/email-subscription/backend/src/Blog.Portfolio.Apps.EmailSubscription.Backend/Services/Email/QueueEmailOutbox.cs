using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public sealed class QueueEmailOutbox : IEmailOutbox
{
    private readonly QueueClient _queueClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public QueueEmailOutbox(QueueClient queueClient, IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        _queueClient = queueClient;
        _jsonSerializerOptions = jsonSerializerOptions.Value;
    }

    public Task EnqueueAsync(SendEmailMessage message, CancellationToken cancellationToken) =>
        _queueClient.SendMessageAsync(JsonSerializer.Serialize(message, _jsonSerializerOptions), cancellationToken);
}

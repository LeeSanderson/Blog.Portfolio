using Azure;
using Azure.Data.Tables;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;

public sealed class SubscriberTableEntity : ITableEntity
{
    public static string PartitionKeyValue => "Subscriber";

    public string PartitionKey { get; set; } = PartitionKeyValue;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

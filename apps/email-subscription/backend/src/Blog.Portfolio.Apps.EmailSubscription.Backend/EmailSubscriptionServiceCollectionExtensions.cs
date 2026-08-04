using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Queues;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.BlogFeed;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Subscribers;
using Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend;

public static class EmailSubscriptionServiceCollectionExtensions
{
    private const string SubscribersTableName = "Subscribers";
    private const string ResendBaseAddress = "https://api.resend.com/";

    public static IServiceCollection AddEmailSubscriptionBackend(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSubscriptionOptions>(options =>
        {
            options.ResendApiKey = configuration["RESEND_API_KEY"] ?? string.Empty;
            options.SigningKey = configuration["EMAIL_SUBSCRIPTION_SIGNING_KEY"] ?? string.Empty;
            options.FromAddress = configuration["EMAIL_SUBSCRIPTION_FROM_ADDRESS"] ?? options.FromAddress;
            options.ConfirmPageUrl = ParseUriOrDefault(configuration["EMAIL_SUBSCRIPTION_CONFIRM_URL"], options.ConfirmPageUrl);
            options.UnsubscribePageUrl =
                ParseUriOrDefault(configuration["EMAIL_SUBSCRIPTION_UNSUBSCRIBE_URL"], options.UnsubscribePageUrl);
        });

        services.AddSingleton(_ =>
        {
            var tableClient = CreateTableServiceClient(configuration).GetTableClient(SubscribersTableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });
        services.AddSingleton(_ =>
        {
            var queueClient = CreateQueueServiceClient(configuration).GetQueueClient(SendEmailMessage.QueueName);
            queueClient.CreateIfNotExists();
            return queueClient;
        });

        services.AddSingleton<ISubscriberStore, TableStorageSubscriberStore>();
        services.AddSingleton<IEmailOutbox, QueueEmailOutbox>();
        services.AddSingleton<ISubscriberTokenService>(serviceProvider =>
            new HmacSubscriberTokenService(
                serviceProvider.GetRequiredService<IOptions<EmailSubscriptionOptions>>().Value.SigningKey));
        services.AddSingleton<SubscriberLinkBuilder>();
        services.AddSingleton<SubscriberLinkAction>();

        services.AddHttpClient<IEmailSender, ResendEmailSender>(
            client => client.BaseAddress = new Uri(ResendBaseAddress));
        services.AddHttpClient<IBlogFeedReader, RssBlogFeedReader>();

        return services;
    }

    private static TableServiceClient CreateTableServiceClient(IConfiguration configuration)
    {
        var connectionString = configuration["AzureWebJobsStorage"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            return new TableServiceClient(connectionString);
        }

        var tableServiceUri = configuration["AzureWebJobsStorage:tableServiceUri"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage__tableServiceUri is not configured.");
        return new TableServiceClient(new Uri(tableServiceUri), CreateStorageCredential(configuration));
    }

    private static QueueServiceClient CreateQueueServiceClient(IConfiguration configuration)
    {
        var connectionString = configuration["AzureWebJobsStorage"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            return new QueueServiceClient(connectionString);
        }

        var queueServiceUri = configuration["AzureWebJobsStorage:queueServiceUri"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage__queueServiceUri is not configured.");
        return new QueueServiceClient(new Uri(queueServiceUri), CreateStorageCredential(configuration));
    }

    private static ManagedIdentityCredential CreateStorageCredential(IConfiguration configuration) =>
        new(configuration["AzureWebJobsStorage:clientId"] ?? configuration["AZURE_CLIENT_ID"]);

    private static Uri ParseUriOrDefault(string? value, Uri fallback) =>
        string.IsNullOrEmpty(value) ? fallback : new Uri(value);
}

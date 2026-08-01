using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Services.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailSubscriptionOptions _options;

    public ResendEmailSender(HttpClient httpClient, IOptions<EmailSubscriptionOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task SendAsync(SendEmailMessage message, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("emails", UriKind.Relative))
        {
            Content = JsonContent.Create(new ResendEmailPayload(
                _options.FromAddress, [message.To], message.Subject, message.HtmlBody)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ResendApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ResendEmailPayload(string From, string[] To, string Subject, string Html);
}

using System.Net;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blog.Portfolio.AppHost.Tests;

public class ExamplePingEndToEndTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);
    private static readonly Uri ExamplePingRoute = new("/api/example/ping", UriKind.Relative);

    [Fact]
    public async Task GetExamplePing_ReturnsPongThroughTheRunningAppHost()
    {
        var cancellationToken = CancellationToken.None;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Blog_Portfolio_AppHost>(cancellationToken);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications
            .WaitForResourceAsync("host", KnownResourceStates.Running, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("host");
        using var response = await GetPingResponseAsync(httpClient, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        body.RootElement.GetProperty("message").GetString().Should().Be("pong");
    }

    private static async Task<HttpResponseMessage> GetPingResponseAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        while (true)
        {
            try
            {
                return await httpClient.GetAsync(ExamplePingRoute, timeoutCts.Token);
            }
            catch (HttpRequestException) when (!timeoutCts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), timeoutCts.Token);
            }
        }
    }
}

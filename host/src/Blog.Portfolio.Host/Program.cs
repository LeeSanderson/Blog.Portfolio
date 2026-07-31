using System.Text.Json;
using System.Text.Json.Serialization;
using Blog.Portfolio.Apps.EmailSubscription.Backend;
using Blog.Portfolio.Host.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddOpenApiDocumentation()
    .AddEmailSubscriptionBackend(builder.Configuration);

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// OpenAPI endpoints are automatically exposed in isolated Azure Functions
// at /api/swagger/ui and /api/openapi/{version}
// CORS for the sixsideddice.com blog frontend is configured at the Function App resource level
// (infra/functionapp.bicep siteConfig.cors), not via ASP.NET Core middleware here — the isolated
// worker's HTTP integration doesn't expose an IApplicationBuilder pipeline to hang UseCors() off.

await app.RunAsync();

using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;

namespace Blog.Portfolio.Host.Tests;

/// <summary>
/// Enforces the app-name route-prefix convention from ADR-0001 by reflecting over the compiled
/// assemblies rather than starting the Functions host or Aspire, so it stays fast and catches
/// violations from any app without a per-app test.
/// </summary>
public class RoutePrefixArchitectureTests
{
    private static readonly Regex AppBackendAssemblyName = new(@"^Blog\.Portfolio\.Apps\.(?<app>.+)\.Backend$");
    private static readonly Regex PascalCaseWordBoundary = new(@"(?<=[a-z0-9])(?=[A-Z])");

    [Fact]
    public void EveryFunctionRoute_StartsWithApiAppNamePrefix()
    {
        var endpoints = DiscoverFunctionEndpoints();

        endpoints.Should().NotBeEmpty(
            "the scan should discover at least one [Function]-attributed endpoint, otherwise this test would pass vacuously");

        foreach (var endpoint in endpoints)
        {
            var expectedPrefix = $"/api/{endpoint.AppName}/";
            endpoint.Route.Should().StartWithEquivalentOf(
                expectedPrefix,
                $"{endpoint.DeclaringType}.{endpoint.MethodName} belongs to app '{endpoint.AppName}' and must route under {expectedPrefix}");
        }
    }

    [Fact]
    public void DiscoverFunctionEndpoints_CatchesARouteMissingTheAppPrefix()
    {
        var endpoints = DiscoverFunctionEndpoints(typeof(RoutePrefixArchitectureTests).Assembly, appName: "example");

        var misconfigured = endpoints.Single(endpoint => endpoint.MethodName == nameof(MisconfiguredExampleFunctions.WrongPrefixAsync));

        var assertRouteConvention = () => misconfigured.Route.Should().StartWithEquivalentOf($"/api/{misconfigured.AppName}/");

        assertRouteConvention.Should().Throw<Exception>(
            "a route missing the required app-name prefix must fail the convention check, proving it actually catches violations");
    }

    // Multi-word app names use kebab-case in their route prefix (e.g. "email-subscription") while the
    // backend project/assembly name is PascalCase (e.g. "EmailSubscription"), so the name extracted from
    // the assembly needs converting before it's compared against the route.
#pragma warning disable S4040 // The kebab-case route convention requires lowercase output, not a culture-comparison
    private static string ToKebabCase(string pascalCaseName) =>
        PascalCaseWordBoundary.Replace(pascalCaseName, "-").ToLowerInvariant();
#pragma warning restore S4040

    private static List<FunctionEndpoint> DiscoverFunctionEndpoints()
    {
        var hostAssembly = typeof(HostAssemblyMarker).Assembly;

        // Per the spec, host/ takes a direct ProjectReference to every app's backend/src project (no transitive
        // wiring), so GetReferencedAssemblies() is sufficient to find every app's backend assembly.
        var appBackendAssemblies = hostAssembly.GetReferencedAssemblies()
            .Select(name => (name, match: AppBackendAssemblyName.Match(name.Name ?? string.Empty)))
            .Where(x => x.match.Success)
            .Select(x => (assembly: Assembly.Load(x.name), appName: ToKebabCase(x.match.Groups["app"].Value)));

        return appBackendAssemblies
            .SelectMany(x => DiscoverFunctionEndpoints(x.assembly, x.appName))
            .ToList();
    }

    private static List<FunctionEndpoint> DiscoverFunctionEndpoints(Assembly assembly, string appName)
    {
        var endpoints = new List<FunctionEndpoint>();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                var function = method.GetCustomAttribute<FunctionAttribute>();
                if (function is null)
                {
                    continue;
                }

                var httpTrigger = method.GetParameters()
                    .Select(parameter => parameter.GetCustomAttribute<HttpTriggerAttribute>())
                    .FirstOrDefault(attribute => attribute is not null);

                if (httpTrigger is null)
                {
                    continue;
                }

                var route = httpTrigger.Route ?? function.Name;
                endpoints.Add(new FunctionEndpoint(
                    Route: $"/api/{route}",
                    AppName: appName,
                    DeclaringType: method.DeclaringType?.FullName ?? assembly.GetName().Name!,
                    MethodName: method.Name));
            }
        }

        return endpoints;
    }

    private sealed record FunctionEndpoint(string Route, string AppName, string DeclaringType, string MethodName);

    /// <summary>
    /// Deliberately misconfigured fixture proving <see cref="DiscoverFunctionEndpoints(Assembly, string)"/> surfaces
    /// a route that violates the app-name prefix convention. Never invoked directly, only reflected over by
    /// <see cref="DiscoverFunctionEndpoints_CatchesARouteMissingTheAppPrefix"/>.
    /// </summary>
#pragma warning disable MA0182 // Only reachable via reflection, not a direct call
    private static class MisconfiguredExampleFunctions
    {
        [Function("WrongPrefix")]
        public static Task<string> WrongPrefixAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "not-example/ping")] object request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult("unused");
        }
    }
#pragma warning restore MA0182
}
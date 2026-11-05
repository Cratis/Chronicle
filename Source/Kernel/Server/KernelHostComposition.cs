// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Cratis.Chronicle.Setup;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Composes listener-first silo startup and the kernel health endpoints.
/// </summary>
internal static class KernelHostComposition
{
    /// <summary>
    /// Defers the Orleans hosted service until listeners bind and registers kernel readiness.
    /// </summary>
    /// <param name="services">The host services.</param>
    /// <exception cref="InvalidOperationException">There is not exactly one Orleans silo hosted service.</exception>
    internal static void UseDeferredSiloStart(this IServiceCollection services)
    {
        var descriptors = services.Where(_ => _.ServiceType == typeof(IHostedService)
            && _.ImplementationType?.FullName == "Orleans.Hosting.SiloHostedService").ToArray();
        if (descriptors.Length != 1)
        {
            throw new InvalidOperationException($"Expected exactly one Orleans silo hosted service, found {descriptors.Length}.");
        }

        services.DeferChronicleDataProtectionWarmup();
        var descriptor = descriptors[0];

        // Keep its position: the web host must still stop before the silo.
        services[services.IndexOf(descriptor)] = ServiceDescriptor.Singleton<IHostedService>(provider =>
        {
            var warmups = provider.GetServices<DataProtectionKeyRingWarmup>().ToArray();
            foreach (var warmup in warmups) warmup.DeferToSiloStart();
            return new DeferredSiloStart(
                (IHostedService)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!),
                provider.GetRequiredService<ILogger<Kernel>>(),
                warmups);
        });
        services.AddSingleton<KernelStartupState>();
        services.AddHealthChecks()
            .AddCheck<KernelReadinessHealthCheck>("kernel-ready", tags: ["ready"])
            .AddCheck<KernelProjectionsRegistrationHealthCheck>("kernel-projections-registration", tags: ["ready"]);
    }

    /// <summary>
    /// Registers storage authorization diagnostics as a readiness check.
    /// </summary>
    /// <param name="services">The host services.</param>
    internal static void AddStorageAuthorizationHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().AddCheck<StorageAuthorizationHealthCheck>("storage-authorization", tags: ["ready"]);

    /// <summary>
    /// Maps readiness checks at the configured endpoint and its ready subpath, and liveness without checks.
    /// </summary>
    /// <param name="app">The kernel web application.</param>
    /// <param name="endpoint">The configured health endpoint.</param>
    internal static void MapKernelHealthChecks(this WebApplication app, string endpoint)
    {
        var path = endpoint.Trim().TrimEnd('/');
        if (path.StartsWith('/')) path = path[1..];
        var prefix = path.Length == 0 ? string.Empty : $"/{path}";
        app.MapHealthChecks(prefix.Length == 0 ? "/" : prefix).AllowAnonymous();
        app.MapHealthChecks($"{prefix}/ready").AllowAnonymous();
        app.MapHealthChecks($"{prefix}/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    }
}

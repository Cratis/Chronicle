// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        var descriptor = descriptors[0];

        // Keep its position: the web host must still stop before the silo.
        services[services.IndexOf(descriptor)] = ServiceDescriptor.Singleton<IHostedService>(provider =>
            new DeferredSiloStart(
                (IHostedService)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!),
                provider.GetRequiredService<ILogger<Kernel>>()));
        services.AddSingleton<KernelStartupState>();
        services.AddHealthChecks().AddCheck<KernelReadinessHealthCheck>("kernel-ready", tags: ["ready"]);
    }

    /// <summary>
    /// Maps readiness checks at the configured endpoint and its ready subpath, and liveness without checks.
    /// </summary>
    /// <param name="app">The kernel web application.</param>
    /// <param name="endpoint">The configured health endpoint.</param>
    internal static void MapKernelHealthChecks(this WebApplication app, string endpoint)
    {
        var path = endpoint.Trim().TrimEnd('/');
        if (!path.StartsWith('/')) path = $"/{path}";
        app.MapHealthChecks(path).AllowAnonymous();
        app.MapHealthChecks($"{path}/ready").AllowAnonymous();
        app.MapHealthChecks($"{path}/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    }
}

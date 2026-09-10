// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for adding the Chronicle <see cref="IHealthCheck"/> to the ASP.NET Core health check pipeline.
/// </summary>
public static class ChronicleHealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// The tags the Chronicle health check is registered with.
    /// </summary>
    /// <remarks>
    /// <c language="csharp">ready</c> is the conventional tag for a readiness probe - a host that cannot reach the kernel can start
    /// and stay alive, but it cannot serve. It is deliberately not tagged for liveness, since restarting the host
    /// does not bring the kernel back.
    /// </remarks>
    public static readonly string[] Tags = ["chronicle", "ready"];

    /// <summary>
    /// Add the Chronicle health check, reporting whether the client holds a live connection to the kernel.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="failureStatus">Optional <see cref="HealthStatus"/> to report when not connected. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Registering a check does not expose an endpoint - the app still maps one, for instance with
    /// <c language="csharp">app.MapHealthChecks("/health/ready", new() { Predicate = _ => _.Tags.Contains("ready") })</c>.
    /// Registration is idempotent, so calling this after it has been added automatically does nothing.
    /// </remarks>
    public static IServiceCollection AddChronicleHealthCheck(this IServiceCollection services, HealthStatus failureStatus = HealthStatus.Unhealthy)
    {
        if (services.Any(_ => _.ServiceType == typeof(HealthCheckRegistrationMarker)))
        {
            return services;
        }

        services.AddSingleton<HealthCheckRegistrationMarker>();
        services.AddHealthChecks()
            .AddCheck<ChronicleHealthCheck>(ChronicleHealthCheck.Name, failureStatus, Tags);

        return services;
    }

    sealed class HealthCheckRegistrationMarker;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Extension methods for configuring and adding open telemetry to Cratis.
/// </summary>
public static class OpenTelemetryConfigurationExtensions
{
    /// <summary>
    /// Add Chronicle instrumentation to the <see cref="MeterProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="MeterProviderBuilder"/> to add to.</param>
    /// <returns>The <see cref="MeterProviderBuilder"/> for continuation.</returns>
    public static MeterProviderBuilder AddChronicleInstrumentation(this MeterProviderBuilder builder)
    {
        builder
            .AddMeter(WellKnown.MeterName)
            .AddMeter("Microsoft.Orleans")
            .AddMeter("Grpc.AspNetCore.Server");

        return builder;
    }

    /// <summary>
    /// Add Chronicle instrumentation to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add to.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> for continuation.</returns>
    public static TracerProviderBuilder AddChronicleInstrumentation(this TracerProviderBuilder builder)
    {
        builder
            .AddSource("Microsoft.Orleans.Runtime")
            .AddSource("Microsoft.Orleans.Application")
            .AddSource(ChronicleActivity.SourceName);

        return builder;
    }

    /// <summary>
    /// Sets up open telemetry for Cratis.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <param name="serviceName">The name of the service exposed on Open Telemetry.</param>
    /// <returns>The builder for continuation.</returns>
    [Obsolete("Use AddOpenTelemetry().WithCratis(configuration, TelemetryConfiguration.Configure) instead.")]
    public static IServiceCollection AddChronicleTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName = "Chronicle")
    {
        services.AddOpenTelemetry().WithCratis(configuration, options =>
        {
            TelemetryConfiguration.Configure(options);
            options.ServiceName = serviceName;
        });
        return services;
    }
}

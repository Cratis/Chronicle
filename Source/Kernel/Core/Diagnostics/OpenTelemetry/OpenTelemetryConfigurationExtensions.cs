// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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
    /// Adds the Chronicle <see cref="Meter{T}"/> and System Diagnostics <see cref="Meter"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddChronicleMeter(this IServiceCollection services)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        services.AddKeyedSingleton(WellKnown.MeterName, new Meter(WellKnown.MeterName));
#pragma warning restore CA2000 // Dispose objects before losing scope
        return services;
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
    public static IServiceCollection AddChronicleTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName = "Chronicle")
    {
        var otlpOptions = configuration.GetSection("OTEL_EXPORTER_OTLP").Get<OtlpExporterOptions>();
        MaybeSetHttp2Unencrypted(otlpOptions);

        var otelBuilder = services.AddOpenTelemetry();

        otelBuilder.ConfigureResource(resources => resources.AddService(serviceName))
            .WithLogging(logger => logger.AddOtlpExporter())
            .WithTracing(tracing =>
            {
                tracing
                    .AddChronicleInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddOtlpExporter();
            })
                .WithMetrics(metrics =>
            {
                metrics
                    .AddChronicleInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter();
            });
        return services;
    }

    static void MaybeSetHttp2Unencrypted(OtlpExporterOptions? options)
    {
        var endpoint = options?.Endpoint.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint) ||
            (Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out var otlpEndpoint) && otlpEndpoint.Scheme.Equals("http")))
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }
}

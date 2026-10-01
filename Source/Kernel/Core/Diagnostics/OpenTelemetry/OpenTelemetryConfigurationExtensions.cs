// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
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
                // Reduce pre-allocated MetricPoint arrays from the default 2000 to 500.
                // The default causes ~25 MB of committed memory at startup for all metric instruments.
                // The limit applies per instrument. Chronicle's observer instruments carry one series per observer
                // in each event store, namespace and event sequence, and never one per partition or event source.
                // The failure instruments only get a series once an observer fails, so the series they hold follow
                // the failing observers, while the successful observations instrument holds one per active observer.
                // Exceeding 500 therefore takes more than 500 active observer instances (observers x namespaces x
                // event stores). Raise the limit before that is reached: the SDK folds the excess into an overflow
                // series that has none of the tags, and those measurements can no longer be attributed to an observer.
                // Do not add a second view for a single instrument: every view that matches an instrument produces
                // its own stream for it, so the instrument would be exported twice. Change this one view instead.
                metrics
                    .AddView("*", new MetricStreamConfiguration { CardinalityLimit = 500 })
                    .AddChronicleInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
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

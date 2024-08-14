// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    /// The <see cref="OpenTelemetryOptions"/> config section path.
    /// </summary>
    public static readonly string ConfigSection = ConfigurationPath.Combine("Cratis", "Chronicle", "OpenTelemetry");

    /// <summary>
    /// Sets up open telemetry for Cratis.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="configureTelemetry">The optional callback for configuring <see cref="OpenTelemetryOptions"/>.</param>
    /// <returns>The builder for continuation.</returns>
    public static IServiceCollection AddChronicleTelemetry(this IServiceCollection services, IConfiguration configuration, Action<OpenTelemetryOptions>? configureTelemetry = default)
    {
        var meter = new Meter("Cratis.Chronicle");
        services.AddSingleton(meter);
        services.AddSingleton(typeof(Meter<>));
        var options = configuration.GetSection(ConfigSection).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
        configureTelemetry?.Invoke(options);

        MaybeSetHttp2Unencrypted(options);
        var otelBuilder = services.AddOpenTelemetry();

        otelBuilder.ConfigureResource(resources => resources.AddService(serviceName: options.ServiceName));
        if (options.Logging)
        {
            otelBuilder.WithLogging(logger => logger.AddOtlpExporter(_ => ConfigureExporter(_, options)));
        }
        if (options.Tracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing
                    .AddSource("Microsoft.Orleans.Runtime")
                    .AddSource("Microsoft.Orleans.Application")
                    .AddSource(ChronicleActivity.SourceName)
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddOtlpExporter(_ => ConfigureExporter(_, options));
            });
        }
        if (options.Metrics)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(meter.Name)
                    .AddMeter("Microsoft.Orleans")
                    .AddMeter("Grpc.AspNetCore.Server")
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(_ => ConfigureExporter(_, options));
            });
        }
        return services;
    }

    static void MaybeSetHttp2Unencrypted(OpenTelemetryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            (Uri.TryCreate(options.Endpoint, UriKind.RelativeOrAbsolute, out var otlpEndpoint) && otlpEndpoint.Scheme.Equals("http")))
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }

    static void ConfigureExporter(OtlpExporterOptions exporterOptions, OpenTelemetryOptions config)
    {
        if (string.IsNullOrWhiteSpace(config.Endpoint))
        {
            return;
        }
        exporterOptions.Endpoint = new Uri(config.Endpoint);
    }
}

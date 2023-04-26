// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Aksio.Cratis.Kernel.Orleans.Configuration;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for working with the <see cref="Telemetry"/> configuration.
/// </summary>
public static class TelemetryConfigurationExtensions
{
    /// <summary>
    /// Use telemetry from configuration.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to extend.</param>
    /// <returns>Builder for continuation.</returns>
    public static ISiloBuilder UseTelemetry(this ISiloBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var telemetryConfig = services.GetTelemetryConfig();
            var meter = new Meter("Cratis.Kernel");
            services.AddSingleton(meter);

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            services
                .AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    switch (telemetryConfig.Type)
                    {
                        case TelemetryTypes.AppInsights:
                            {
                                var options = telemetryConfig.GetAppInsightsTelemetryOptions();
                                if (!string.IsNullOrEmpty(options.ConnectionString))
                                {
                                    tracing.AddAzureMonitorTraceExporter(exporter => exporter.ConnectionString = options.ConnectionString);
                                }
                            }
                            break;

                        case TelemetryTypes.OpenTelemetry:
                            {
                                var openTelemetryOptions = telemetryConfig.GetOpenTelemetryOptions();
                                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(openTelemetryOptions.Endpoint));
                            }
                            break;
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddMeter(meter.Name)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddInMemoryObservableMetrics();

                    switch (telemetryConfig.Type)
                    {
                        case TelemetryTypes.AppInsights:
                            {
                                var options = telemetryConfig.GetAppInsightsTelemetryOptions();
                                if (!string.IsNullOrEmpty(options.ConnectionString))
                                {
                                    metrics.AddAzureMonitorMetricExporter(exporter => exporter.ConnectionString = options.ConnectionString);
                                }
                            }
                            break;

                        case TelemetryTypes.OpenTelemetry:
                            {
                                var openTelemetryOptions = telemetryConfig.GetOpenTelemetryOptions();
                                metrics.AddOtlpExporter(options => options.Endpoint = new Uri(openTelemetryOptions.Endpoint));
                            }
                            break;
                    }
                });

            switch (telemetryConfig.Type)
            {
                case TelemetryTypes.AppInsights:
                    {
                        // builder.AddApplicationInsightsTelemetryConsumer(options.Key);
                        var options = telemetryConfig.GetAppInsightsTelemetryOptions();
                    }
                    break;
            }
        });

        return builder;
    }
}

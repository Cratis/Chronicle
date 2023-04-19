// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Aksio.Cratis.Kernel.Orleans.Configuration;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

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

            switch (telemetryConfig.Type)
            {
                case TelemetryTypes.AppInsights:
                    var options = telemetryConfig.GetAppInsightsTelemetryOptions();
                    builder.AddApplicationInsightsTelemetryConsumer(options.Key);

                    if (!string.IsNullOrEmpty(options.ConnectionString))
                    {
                        services
                            .AddOpenTelemetry()
                            .WithMetrics(metrics => metrics
                                .AddMeter(meter.Name)
                                .AddAspNetCoreInstrumentation()
                                .AddHttpClientInstrumentation()
                                .AddRuntimeInstrumentation()
                                .AddAzureMonitorMetricExporter(exporter => exporter.ConnectionString = options.ConnectionString));
                    }
                    break;
            }
        });

        return builder;
    }
}

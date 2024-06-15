// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Extension methods for working with the telemetry configuration.
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
            var meter = new Meter("Cratis.Chronicle");
            services.AddSingleton(meter);
            services.AddSingleton(typeof(Meter<>));

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            services
                .AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource("Microsoft.Orleans.Runtime")
                        .AddSource("Microsoft.Orleans.Application")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddMeter(meter.Name)
                        .AddMeter("Microsoft.Orleans")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                });
        });

        return builder;
    }
}

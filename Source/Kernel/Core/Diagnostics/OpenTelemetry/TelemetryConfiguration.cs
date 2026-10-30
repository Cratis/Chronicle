// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Chronicle defaults and instrumentation for the shared telemetry setup.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>
    /// Configures Chronicle instrumentation without overriding standard OTEL resource or export settings.
    /// </summary>
    /// <param name="options">The shared telemetry options.</param>
    public static void Configure(CratisOpenTelemetryOptions options)
    {
        options.ServiceName = "Chronicle";
        options.IncludeExceptionDetails = true;
        options.ConfigureTracing = tracing => tracing
            .AddChronicleInstrumentation()
            .AddGrpcClientInstrumentation();
        options.ConfigureMetrics = ConfigureMetrics;
    }

    static void ConfigureMetrics(MeterProviderBuilder metrics)
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
            .AddChronicleInstrumentation();
    }
}

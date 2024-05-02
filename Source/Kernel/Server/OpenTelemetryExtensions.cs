// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Metrics;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Extension methods for configuring the observable in-memory metrics.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Gets the internal metrics collection.
    /// </summary>
    internal static readonly MetricCollection Metrics = [];

    /// <summary>
    /// Add in-memory observable metrics.
    /// </summary>
    /// <param name="builder">The <see cref="MeterProviderBuilder"/> to add to.</param>
    /// <returns><see cref="MeterProviderBuilder"/> for continuation.</returns>
    public static MeterProviderBuilder AddInMemoryObservableMetrics(this MeterProviderBuilder builder) =>
        builder.AddInMemoryExporter(
            Metrics,
            options =>
            {
                options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                options.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = 1000;
            });
}

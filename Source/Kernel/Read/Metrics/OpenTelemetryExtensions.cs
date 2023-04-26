// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OpenTelemetry.Metrics;

/// <summary>
/// Extension methods for configuring the observable in-memory metrics.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Add in-memory observable metrics.
    /// </summary>
    /// <param name="builder">The <see cref="MeterProviderBuilder"/> to add to.</param>
    /// <returns><see cref="MeterProviderBuilder"/> for continuation.</returns>
    public static MeterProviderBuilder AddInMemoryObservableMetrics(this MeterProviderBuilder builder) =>
        builder.AddInMemoryExporter(
            Aksio.Cratis.Kernel.Read.Metrics.Metrics._metrics,
            options =>
            {
                options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                options.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = 1000;
            });
}

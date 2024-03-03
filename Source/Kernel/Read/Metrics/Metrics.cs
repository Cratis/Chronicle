// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;

namespace Cratis.Kernel.Read.Metrics;

/// <summary>
/// Represents the API for working with metrics.
/// </summary>
[Route("/api/metrics")]
public class Metrics : ControllerBase
{
    /// <summary>
    /// Gets the internal collection of metrics.
    /// </summary>
    internal static MetricCollection _metrics = new();

    /// <summary>
    /// Observe all metrics.
    /// </summary>
    /// <returns>A <see cref="ClientObservable{T}"/> of a collection of <see cref="Metric"/>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<MetricMeasurement>>> AllMetrics()
    {
        var metricsObservable = new ClientObservable<IEnumerable<MetricMeasurement>>();

        void contentChanged()
        {
            metricsObservable.OnNext(_metrics.Measurements.OrderBy(_ => _.Name));
        }

        _metrics.ContentChanged += contentChanged;
        metricsObservable.ClientDisconnected += () => _metrics.ContentChanged -= contentChanged;
        return Task.FromResult(metricsObservable);
    }
}

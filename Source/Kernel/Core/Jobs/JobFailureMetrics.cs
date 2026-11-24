// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobFailureMetrics"/> that publishes the failed jobs counter.
/// </summary>
/// <remarks>
/// The instrument is new and has no legacy name, so it is published under the convention name whatever
/// Telemetry:MetricNames selects. The only tag is the job type, a fixed list.
/// </remarks>
[Singleton]
public sealed class JobFailureMetrics : IJobFailureMetrics
{
    readonly Counter<long>? _failed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobFailureMetrics"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> owning the instrument.</param>
    public JobFailureMetrics([FromKeyedServices(WellKnown.MeterName)] IMeter<JobFailureMetrics> meter)
        : this(meter.ActualMeter)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobFailureMetrics"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="Meter"/> owning the instrument, if any.</param>
    internal JobFailureMetrics(Meter? meter) =>
        _failed = meter?.CreateCounter<long>(
            WellKnownMetricNames.JobFailed,
            WellKnownMetricNames.JobFailedUnit,
            "Number of jobs that ended with failed steps or could not prepare their steps, by job type");

    /// <inheritdoc/>
    public void Failed(string jobType) => _failed?.Add(1, new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.JobType, jobType));
}

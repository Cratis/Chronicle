// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Records successful observations for an observer without creating its other instruments.
/// </summary>
/// <param name="metrics">The <see cref="MetricSet"/> scoped to the observer.</param>
/// <remarks>
/// Catch-up and replay job steps record only successful observations. A large replay activates a step per partition,
/// so creating just this instrument keeps the per-step cost to one instrument lookup rather than one per observer
/// instrument. It publishes the same instrument as <see cref="ObserverMetrics"/>.
/// </remarks>
internal sealed class ObserverObservationMetrics(MetricSet metrics)
{
    static readonly object _replay = true;
    static readonly object _notReplay = false;

    readonly MetricInstrument<int, CounterMetricRecorder<int>> _successful = SuccessfulObservations(metrics);

    /// <summary>
    /// Creates the successful observations instrument.
    /// </summary>
    /// <param name="metrics">The <see cref="MetricSet"/> scoped to the observer.</param>
    /// <returns>The instrument.</returns>
    internal static MetricInstrument<int, CounterMetricRecorder<int>> SuccessfulObservations(MetricSet metrics) =>
        metrics.Counter<int>(
            WellKnownMetricNames.ObserverSuccessfulObservations,
            WellKnownMetricNames.ObserverSuccessfulObservationsUnit,
            "Number of successful observations per observer in a given event store and namespace",
            WellKnownTelemetryAttributes.Replay);

    /// <summary>
    /// Gets the boxed value of the replay attribute.
    /// </summary>
    /// <param name="replay">Whether the observation was part of a replay.</param>
    /// <returns>The cached boxed value.</returns>
    internal static object ReplayValue(bool replay) => replay ? _replay : _notReplay;

    /// <summary>
    /// Records one successful observation.
    /// </summary>
    /// <param name="replay">Whether the observation was part of a replay.</param>
    internal void SuccessfulObservation(bool replay) => _successful.Record(1, ReplayValue(replay));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Records the observations this step makes in the observer's metrics, so partition catch-up, replay and retries are
/// not invisible.
/// </summary>
public partial class HandleEventsForPartition
{
    ObserverObservationMetrics? _metrics;

    /// <summary>
    /// Records one successful observation for the observer, marked as replay when the step replays.
    /// </summary>
    /// <param name="currentState">The state of the step.</param>
    /// <remarks>
    /// Counted once per successful subscriber call, like the live path, which this step never goes through, so
    /// nothing is counted twice.
    /// </remarks>
    void RecordSuccessfulObservation(HandleEventsForPartitionState currentState)
    {
        if (observerMeter is null) return;
        _metrics ??= observerMeter.BeginObservationScope(
            currentState.ObserverKey,
            options?.Value?.Telemetry.MetricNames ?? MetricNames.Legacy);
        var retry = currentState.EpisodeId is not null || currentState.DeferralId is not null;
        _metrics.SuccessfulObservation(replay: !retry && currentState.EventObservationState.ToObservationOrigin() == ObservationOrigin.Replay);
    }
}

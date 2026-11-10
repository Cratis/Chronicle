// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Records the observations this step makes in the observer's metrics, so catch-up and replay are not invisible.
/// </summary>
public partial class HandleEventsForObserver
{
    /// <summary>
    /// Records one successful observation for the observer, marked as replay when the step replays.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the step handles events for.</param>
    /// <remarks>
    /// Counted once per successful subscriber call and partition, like the live path, which this step never goes
    /// through, so nothing is counted twice.
    /// </remarks>
    void RecordSuccessfulObservation(ObserverKey observerKey)
    {
        if (observerMeter is null) return;
        _metrics ??= observerMeter.BeginObserverScope(
            observerKey.ObserverId,
            observerKey,
            _options.Value?.Telemetry.MetricNames ?? MetricNames.Legacy);
        _metrics.SuccessfulObservation(replay: _origin == ObservationOrigin.Replay);
    }
}

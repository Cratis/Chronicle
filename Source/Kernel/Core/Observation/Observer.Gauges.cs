// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Reports the observer's current state to the <see cref="IObserverGauges"/>.
/// </summary>
/// <remarks>
/// Snapshots are published where the observer's state is persisted or transitions, never per delivered event. Only an
/// activation that owns a live observer reports; any other activation releases what it registered.
/// </remarks>
public partial class Observer
{
    ObserverGaugeEntry? _gauge;

    bool ReportsGauges => !_removed && _observerExists && KeyOwnsDefinition;

    /// <summary>
    /// Publishes the running state, when it changed since the last snapshot.
    /// </summary>
    void PublishRunningStateGauge()
    {
        if (!EnsureGauge(out var gauge)) return;
        var snapshot = gauge.Snapshot;
        if (snapshot.RunningState == State.RunningState) return;
        gauge.Publish(snapshot with { RunningState = State.RunningState });
    }

    /// <summary>
    /// Publishes the failed and quarantined partition counts together with the running state.
    /// </summary>
    void PublishFailedPartitionsGauge()
    {
        if (!EnsureGauge(out var gauge)) return;
        gauge.Publish(CurrentGaugeSnapshot());
    }

    bool EnsureGauge(out ObserverGaugeEntry gauge)
    {
        gauge = null!;
        if (observerGauges is null) return false;
        if (!ReportsGauges)
        {
            ReleaseGauge();
            return false;
        }

        if (_gauge is not null && _gauge.ObserverType != Definition.Type) ReleaseGauge();
        if (_gauge is null)
        {
            // A registry substituted in a test host may hand back nothing; the observer then simply does not report.
            if (observerGauges.Register(_observerKey, Definition.Type) is not { } registered) return false;
            _gauge = registered;
            _gauge.Publish(CurrentGaugeSnapshot());
        }

        gauge = _gauge;
        return true;
    }

    ObserverGaugeSnapshot CurrentGaugeSnapshot()
    {
        var failed = 0;
        var quarantined = 0;
        foreach (var partition in Failures.Partitions)
        {
            if (partition.IsResolved) continue;
            failed++;
            if (partition.IsQuarantined) quarantined++;
        }

        return new(State.RunningState, failed, quarantined);
    }

    void ReleaseGauge()
    {
        _gauge?.Release();
        _gauge = null;
    }
}

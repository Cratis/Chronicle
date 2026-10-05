// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Samples incident reactor health from its native observation state.
/// </summary>
/// <param name="grainFactory">The observer grain factory.</param>
/// <param name="storage">The storage registry.</param>
public class AlertIncidentsReadiness(IGrainFactory grainFactory, IStorage storage) : IAlertIncidentsReadiness
{
    static readonly EventType[] _transitionTypes = [typeof(AlertRaised).GetEventType(), typeof(AlertEscalated).GetEventType(), typeof(AlertCleared).GetEventType()];

    /// <inheritdoc/>
    public async Task<AlertIncidentsReadinessState> Get()
    {
        var namespaceStorage = storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default);

        // IEventSequenceStorage.GetTailSequenceNumber propagates read errors, unlike the grain's unavailable fallback.
        var tail = await namespaceStorage.GetEventSequence(EventSequenceId.System).GetTailSequenceNumber(eventTypes: _transitionTypes);

        // IObserver.GetState is sampled after the tail; this is health, not a cross-partition checkpoint.
        var observer = await grainFactory.GetGrain<IObserver>(AlertIncidentsReactor.ObserverKey).GetState();

        // IFailedPartitionsStorage.GetFor includes quarantined partitions of this observer only.
        var failures = await namespaceStorage.FailedPartitions.GetFor(AlertIncidentsReactor.ObserverKey.ObserverId);

        return Evaluate(observer, failures, tail);
    }

    /// <summary>
    /// Evaluates a sampled tail and native observer health, ignoring normal in-flight work.
    /// </summary>
    /// <param name="observer">Native observer state.</param>
    /// <param name="failures">Failures of the incidents observer only.</param>
    /// <param name="tail">Filtered transition tail.</param>
    /// <returns>The sampled health.</returns>
    internal static AlertIncidentsReadinessState Evaluate(ObserverState observer, FailedPartitions failures, EventSequenceNumber tail)
    {
        if (failures.HasFailedPartitions)
        {
            return AlertIncidentsReadinessState.Degraded;
        }
        if (observer.RunningState != ObserverRunningState.Active || observer.CatchingUpPartitions.Count != 0 ||
            observer.ReplayingPartitions.Count != 0 || observer.IsReplaying)
        {
            return AlertIncidentsReadinessState.CatchingUp;
        }

        return tail == EventSequenceNumber.Unavailable || observer.NextEventSequenceNumber > tail
            ? AlertIncidentsReadinessState.Ready
            : AlertIncidentsReadinessState.CatchingUp;
    }
}

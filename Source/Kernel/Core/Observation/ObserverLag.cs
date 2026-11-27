// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the latest measurement of how far an observer is from the end of its event sequence.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The canonical namespace of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="Status">What the measurement says about the observer.</param>
/// <param name="Cause">Why it says that.</param>
/// <param name="TailEventSequenceNumber">The end of the event sequence when measured, or null when it was not read.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer expected, or null when it is not an actual position.</param>
/// <param name="LastHandledEventSequenceNumber">The event sequence number the observer last handled, or null when it has handled none.</param>
/// <param name="RelevantPendingEventSequenceNumber">The first event the observer observes at or after its cursor, or null when none is known.</param>
/// <param name="Distance">How many event sequence slots lie between the cursor and the end of the sequence, or null when it could not be measured. Zero means caught up; it is not the same as null.</param>
/// <param name="SampledAt">When the measurement was taken, or null when there never was one.</param>
/// <param name="ExpiresAt">When the measurement stops describing the observer, or null when there never was one.</param>
[ReadModel]
[BelongsTo(WellKnownServices.ObserverProgress)]
[StoreCapability("chronicle.event-store.read")]
public record ObserverLag(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string ObserverId,
    ObserverLagStatus Status,
    ObserverLagCause Cause,
    ulong? TailEventSequenceNumber,
    ulong? NextEventSequenceNumber,
    ulong? LastHandledEventSequenceNumber,
    ulong? RelevantPendingEventSequenceNumber,
    ulong? Distance,
    DateTimeOffset? SampledAt,
    DateTimeOffset? ExpiresAt)
{
    /// <summary>
    /// Gets the latest measurement of one observer.
    /// </summary>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespace">The namespace of the observer.</param>
    /// <param name="eventSequenceId">The event sequence the observer observes; empty for the one its definition names.</param>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the observer's definition and state.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> reaching a resident observer.</param>
    /// <param name="dormantObservers">The <see cref="IDormantObservers"/> that tells a dormant observer from a resident one without waking it.</param>
    /// <returns>The <see cref="ObserverLag"/>, which says nothing can be measured rather than guessing.</returns>
    /// <remarks>
    /// Reading never starts or resets a stall window, and never gives an observer an activation: an observer that is
    /// unknown, never started or dormant has no measurement to read. The resident observer decides whether its own
    /// measurement has expired.
    /// </remarks>
    public static async Task<ObserverLag> GetObserverLag(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string eventSequenceId,
        ObserverId observerId,
        IStorage storage,
        IGrainFactory grainFactory,
        IDormantObservers dormantObservers)
    {
        var sequence = await ObserverEventSequenceResolver.Resolve(storage, eventStore, observerId, eventSequenceId);
        var key = new ObserverKey(observerId, eventStore, @namespace, sequence);

        if (!await storage.GetEventStore(eventStore).Observers.Has(observerId))
        {
            return key.ToLag(ObserverLagSample.Unmeasurable(ObserverLagCause.UnknownState));
        }

        if (await dormantObservers.Find(key) is not null)
        {
            return key.ToLag(ObserverLagSample.NotSampled);
        }

        var state = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers.Get(observerId);
        if (state.RunningState == ObserverRunningState.Unknown)
        {
            return key.ToLag(ObserverLagSample.Unmeasurable(ObserverLagCause.UnknownState));
        }

        var information = await grainFactory.GetGrain<IObserver>(key).GetInformation();
        return key.ToLag(information.LagSample ?? ObserverLagSample.NotSampled);
    }
}

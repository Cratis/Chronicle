// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ObserverStateGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var eventStoreNamespace = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace);
        var observers = eventStoreNamespace.Observers;
        var failedPartitionsStorage = eventStoreNamespace.FailedPartitions;
        var failedPartitions = await failedPartitionsStorage.GetFor(observerKey.ObserverId);
        var actualFailedPartitions = failedPartitions.Partitions.ToArray();
        var storedState = await observers.Get(observerKey.ObserverId);
        var recordExists = IsStoredRecord(storedState);
        actualGrainState.RecordExists = recordExists;

        // The observer mutates its partition sets in place, so it must own them - never share them with the storage
        // or with another observer's state.
        actualGrainState.State = storedState with
        {
            // Preserve a missing record's sentinel until the observer activation supplies its identity.
            Identifier = recordExists ? observerKey.ObserverId : ObserverId.Unspecified,
            ReplayingPartitions = new HashSet<Key>(storedState.ReplayingPartitions),
            CatchingUpPartitions = new HashSet<Key>(storedState.CatchingUpPartitions),
            InFlightPartitions = new HashSet<Key>(storedState.InFlightPartitions),
            FailedPartitions = actualFailedPartitions,
            FailedPartitionCount = actualFailedPartitions.Length
        };
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Observers;
        await observers.Save(actualGrainState.State!);
    }

    /// <summary>
    /// Gets whether the stored state is an actual record, rather than the empty state the storage reports
    /// when no record exists.
    /// </summary>
    /// <param name="state">The state as reported by the storage.</param>
    /// <returns>True when the state carries anything a stored record carries; false when it is empty.</returns>
    /// <remarks>
    /// <see cref="ObserverState.Empty"/> is a per-access factory and the record's synthesized equality compares
    /// the partition sets by reference, so emptiness is recognized field by field rather than by equality
    /// against a fresh empty state.
    /// </remarks>
    static bool IsStoredRecord(ObserverState state) =>
        state.Identifier != ObserverId.Unspecified
        || state.LastHandledEventSequenceNumber.IsActualValue
        || state.RunningState != ObserverRunningState.Unknown
        || state.FailedPartitionCount != FailedPartitionCount.Zero
        || state.IsReplaying
        || state.SubscribesToAllEvents
        || state.ReplayingPartitions.Count > 0
        || state.CatchingUpPartitions.Count > 0
        || state.InFlightPartitions.Count > 0
        || state.FailedPartitions.Any();
}

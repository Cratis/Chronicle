// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Orleans.Storage;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event sequence state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
public class EventSequencesStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// The persisted state is only a warm-start snapshot — the event sequence no longer writes it on every append.
    /// The authoritative source is the event tail: <see cref="EventSequenceState.SequenceNumber"/> is always rebuilt
    /// from the actual tail here, and the per-event-type tails are rebuilt from the events whenever the persisted
    /// snapshot is stale (a crash lost appends since the last periodic write) or missing, so no sequence-number
    /// correctness is lost between periodic writes.
    /// </remarks>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventSequenceState>)!;
        var key = EventSequenceKey.Parse(grainId.Key.ToString()!);

        var eventTypesStorage = storage.GetEventStore(key.EventStore).EventTypes;
        var eventSequenceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).GetEventSequence(key.EventSequenceId);
        actualGrainState.State = await eventSequenceStorage.GetState();
        var persistedSequenceNumber = actualGrainState.State.SequenceNumber;
        await SetNextSequenceNumberFromActualTail(eventSequenceStorage, actualGrainState);
        await RebuildTailSequenceNumbersForEventTypesIfStale(eventTypesStorage, eventSequenceStorage, actualGrainState, persistedSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = EventSequenceKey.Parse(grainId.Key.ToString()!);
        var eventSequenceState = (grainState.State as EventSequenceState)!;
        var eventSequenceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).GetEventSequence(key.EventSequenceId);
        await eventSequenceStorage.SaveState(eventSequenceState);
    }

    async Task RebuildTailSequenceNumbersForEventTypesIfStale(
        IEventTypesStorage eventTypesStorage,
        IEventSequenceStorage eventSequenceStorage,
        IGrainState<EventSequenceState> actualGrainState,
        EventSequenceNumber persistedSequenceNumber)
    {
        var state = actualGrainState.State;
        if (state.SequenceNumber <= EventSequenceNumber.First)
        {
            return;
        }

        // The persisted per-event-type tails are only trustworthy when the snapshot was written at the current tail —
        // a clean deactivation or the most recent periodic write. If the actual tail has moved past the persisted
        // sequence number, appends were lost to a crash between periodic writes, so the tails are rebuilt from the events.
        var snapshotIsCurrent = state.TailSequenceNumberPerEventType.Count > 0 && persistedSequenceNumber == state.SequenceNumber;
        if (snapshotIsCurrent)
        {
            return;
        }

        var eventSchemas = await eventTypesStorage.GetLatestForAllEventTypes();
        var eventTypes = eventSchemas.Select(_ => _.Type).ToArray();
        var sequenceNumbers = await eventSequenceStorage.GetTailSequenceNumbersForEventTypes(eventTypes);
        state.TailSequenceNumberPerEventType = sequenceNumbers
                                                    .Where(_ => _.Value != EventSequenceNumber.Unavailable)
                                                    .ToDictionary(_ => _.Key.Id, _ => _.Value);
    }

    async Task SetNextSequenceNumberFromActualTail(IEventSequenceStorage eventSequenceStorage, IGrainState<EventSequenceState> actualGrainState)
    {
        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber();
        actualGrainState.State.SequenceNumber = tailSequenceNumber == EventSequenceNumber.Unavailable
            ? EventSequenceNumber.First
            : tailSequenceNumber.Next();
    }
}

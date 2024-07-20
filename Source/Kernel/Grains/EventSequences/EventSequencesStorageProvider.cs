// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.EventSequences;

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
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventSequenceState>)!;
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);

        var eventTypesStorage = storage.GetEventStore(key.EventStore).EventTypes;
        var eventSequenceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).GetEventSequence(eventSequenceId.ToString());
        actualGrainState.State = await eventSequenceStorage.GetState();
        await HandleTailSequenceNumbersForEventTypes(eventTypesStorage, eventSequenceStorage, actualGrainState);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);
        var eventSequenceState = (grainState.State as EventSequenceState)!;
        var eventSequenceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).GetEventSequence(eventSequenceId.ToString());
        await eventSequenceStorage.SaveState(eventSequenceState);
    }

    async Task HandleTailSequenceNumbersForEventTypes(IEventTypesStorage eventTypesStorage, IEventSequenceStorage eventSequenceStorage, IGrainState<EventSequenceState> actualGrainState)
    {
        if (actualGrainState.State.SequenceNumber > EventSequenceNumber.First &&
            actualGrainState.State.TailSequenceNumberPerEventType.Count == 0)
        {
            var eventSchemas = await eventTypesStorage.GetLatestForAllEventTypes();
            var eventTypes = eventSchemas.Select(_ => _.Type).ToArray();
            var sequenceNumbers = await eventSequenceStorage.GetTailSequenceNumbersForEventTypes(eventTypes);
            actualGrainState.State.TailSequenceNumberPerEventType = sequenceNumbers
                                                                        .Where(_ => _.Value != EventSequenceNumber.Unavailable)
                                                                        .ToDictionary(_ => _.Key.Id, _ => _.Value);
            await eventSequenceStorage.SaveState(actualGrainState.State);
        }
    }
}

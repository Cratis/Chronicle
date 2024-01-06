// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event sequence state storage.
/// </summary>
public class EventSequencesStorageProvider : IGrainStorage
{
    readonly IClusterStorage _clusterStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing storage for the cluster.</param>
    public EventSequencesStorageProvider(IClusterStorage clusterStorage)
    {
        _clusterStorage = clusterStorage;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventSequenceState>)!;
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);

        var eventTypesStorage = _clusterStorage.GetEventStore((string)key.MicroserviceId).EventTypes;
        var eventSequenceStorage = _clusterStorage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).GetEventSequence(eventSequenceId);
        actualGrainState.State = await eventSequenceStorage.GetState();
        await HandleTailSequenceNumbersForEventTypes(eventTypesStorage, eventSequenceStorage, actualGrainState);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);
        var eventSequenceState = (grainState.State as EventSequenceState)!;
        var eventSequenceStorage = _clusterStorage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).GetEventSequence(eventSequenceId);
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

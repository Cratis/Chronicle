// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.DependencyInversion;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event sequence state storage.
/// </summary>
public class EventSequencesStorageProvider : IGrainStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventTypesStorage> _eventTypesStorageProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventTypesStorageProvider">Provider for <see cref="IEventTypesStorage"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    public EventSequencesStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventTypesStorage> eventTypesStorageProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _eventTypesStorageProvider = eventTypesStorageProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventSequenceState>)!;
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        actualGrainState.State = await _eventSequenceStorageProvider().GetState();
        await HandleTailSequenceNumbersForEventTypes(actualGrainState);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        var eventSequenceState = (grainState.State as EventSequenceState)!;
        await _eventSequenceStorageProvider().SaveState(eventSequenceState);
    }

    async Task HandleTailSequenceNumbersForEventTypes(IGrainState<EventSequenceState> actualGrainState)
    {
        if (actualGrainState.State.SequenceNumber > EventSequenceNumber.First &&
            actualGrainState.State.TailSequenceNumberPerEventType.Count == 0)
        {
            var eventSchemas = await _eventTypesStorageProvider().GetLatestForAllEventTypes();
            var eventTypes = eventSchemas.Select(_ => _.Type).ToArray();
            var sequenceNumbers = await _eventSequenceStorageProvider().GetTailSequenceNumbersForEventTypes(eventTypes);
            actualGrainState.State.TailSequenceNumberPerEventType = sequenceNumbers
                                                                        .Where(_ => _.Value != EventSequenceNumber.Unavailable)
                                                                        .ToDictionary(_ => _.Key.Id, _ => _.Value);
            await _eventSequenceStorageProvider().SaveState(actualGrainState.State);
        }
    }
}

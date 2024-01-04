// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.DependencyInversion;
using MongoDB.Driver;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event sequence state storage.
/// </summary>
public class EventSequencesStorageProvider : IGrainStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventStoreInstanceDatabase> _eventStoreDatabaseProvider;
    readonly ProviderFor<IEventTypesStorage> _schemaStoreProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreInstanceDatabase"/>.</param>
    /// <param name="schemaStoreProvider">Provider for <see cref="IEventTypesStorage"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    public EventSequencesStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreInstanceDatabase> eventStoreDatabaseProvider,
        ProviderFor<IEventTypesStorage> schemaStoreProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _schemaStoreProvider = schemaStoreProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
    }

    IMongoCollection<EventSequenceState> Collection => _eventStoreDatabaseProvider().GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<Storage.EventSequences.EventSequenceState>)!;
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);

        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        var cursor = await Collection.FindAsync(filter);
        var state = await cursor.FirstOrDefaultAsync();
        actualGrainState.State = state?.ToKernel() ?? new Storage.EventSequences.EventSequenceState();

        await HandleTailSequenceNumbersForEventTypes(actualGrainState, eventSequenceId);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = EventSequenceKey.Parse(keyAsString!);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        var eventSequenceState = (grainState.State as Storage.EventSequences.EventSequenceState)!;
        await Write(eventSequenceId, eventSequenceState);
    }

    async Task HandleTailSequenceNumbersForEventTypes(IGrainState<Storage.EventSequences.EventSequenceState> actualGrainState, EventSequenceId eventSequenceId)
    {
        if (actualGrainState.State.SequenceNumber > EventSequenceNumber.First &&
            actualGrainState.State.TailSequenceNumberPerEventType.Count == 0)
        {
            var eventSchemas = await _schemaStoreProvider().GetLatestForAllEventTypes();
            var eventTypes = eventSchemas.Select(_ => _.Type).ToArray();
            var sequenceNumbers = await _eventSequenceStorageProvider().GetTailSequenceNumbersForEventTypes(eventSequenceId, eventTypes);
            actualGrainState.State.TailSequenceNumberPerEventType = sequenceNumbers
                                                                        .Where(_ => _.Value != EventSequenceNumber.Unavailable)
                                                                        .ToDictionary(_ => _.Key.Id, _ => _.Value);
            await Write(eventSequenceId, actualGrainState.State);
        }
    }

    async Task Write(EventSequenceId eventSequenceId, Storage.EventSequences.EventSequenceState state)
    {
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        await Collection.ReplaceOneAsync(
            filter,
            state.ToMongoDB(),
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}

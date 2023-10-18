// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Kernel.Schemas;
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
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="schemaStoreProvider">Provider for <see cref="ISchemaStore"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    public EventSequencesStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ProviderFor<ISchemaStore> schemaStoreProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _schemaStoreProvider = schemaStoreProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
    }

    IMongoCollection<EventSequenceState> Collection => _eventStoreDatabaseProvider().GetCollection<EventSequenceState>(CollectionNames.EventSequences);

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventSequenceState>)!;
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = MicroserviceAndTenant.Parse(keyAsString!);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);

        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        var cursor = await Collection.FindAsync(filter);
        actualGrainState.State = await cursor.FirstOrDefaultAsync() ?? new EventSequenceState();

        var eventSchemas = await _schemaStoreProvider().GetLatestForAllEventTypes();
        var eventTypes = eventSchemas.Select(_ => _.Type).ToArray();
        var sequenceNumbers = await _eventSequenceStorageProvider().GetTailSequenceNumbersForEventTypes(eventSequenceId, eventTypes);
        actualGrainState.State.TailSequenceNumberPerEventType = sequenceNumbers.ToDictionary(_ => _.Key.Id, _ => _.Value);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var eventSequenceId = grainId.GetGuidKey(out var keyAsString);
        var key = MicroserviceAndTenant.Parse(keyAsString!);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        var eventLogState = grainState.State as EventSequenceState;
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        await Collection.UpdateOneAsync(
            filter,
            Builders<EventSequenceState>.Update.Set(_ => _.SequenceNumber, eventLogState!.SequenceNumber),
            new() { IsUpsert = true }).ConfigureAwait(false);
    }
}

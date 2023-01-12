// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event sequence state storage.
/// </summary>
public class EventSequencesStorageProvider : IGrainStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    IMongoCollection<EventSequenceState> Collection => _eventStoreDatabaseProvider().GetCollection<EventSequenceState>(CollectionNames.EventSequences);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public EventSequencesStorageProvider(IExecutionContextManager executionContextManager, ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _executionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var eventSequenceId = grainReference.GetPrimaryKey(out var keyAsString);
        var key = MicroserviceAndTenant.Parse(keyAsString);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        var cursor = await Collection.FindAsync(filter);
        grainState.State = await cursor.FirstOrDefaultAsync() ?? new EventSequenceState();
    }

    /// <inheritdoc/>
    public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var eventSequenceId = grainReference.GetPrimaryKey(out var keyAsString);
        var key = MicroserviceAndTenant.Parse(keyAsString);
        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        var eventLogState = grainState.State as EventSequenceState;
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        return Collection.UpdateOneAsync(
            filter,
            Builders<EventSequenceState>.Update.Set(_ => _.SequenceNumber, eventLogState!.SequenceNumber),
            new() { IsUpsert = true });
    }
}

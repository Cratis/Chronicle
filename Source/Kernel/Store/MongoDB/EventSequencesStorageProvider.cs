// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event log state storage.
/// </summary>
public class EventSequencesStorageProvider : IGrainStorage
{
    const string CollectionName = "event-sequences";

    readonly IExecutionContextManager _executionContextManager;
    readonly IEventStoreDatabase _eventStoreDatabase;

    IMongoCollection<EventSequenceState> Collection => _eventStoreDatabase.GetCollection<EventSequenceState>(CollectionName);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencesStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabase">Provider for <see cref="IMongoDatabase"/>.</param>
    public EventSequencesStorageProvider(IExecutionContextManager executionContextManager, IEventStoreDatabase eventStoreDatabase)
    {
        _executionContextManager = executionContextManager;
        _eventStoreDatabase = eventStoreDatabase;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var eventLogId = grainReference.GetPrimaryKey(out var tenantIdAsString);
        _executionContextManager.Establish(Guid.Parse(tenantIdAsString), string.Empty);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventLogId);
        var cursor = await Collection.FindAsync(filter);
        grainState.State = await cursor.FirstOrDefaultAsync() ?? new EventSequenceState();
    }

    /// <inheritdoc/>
    public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var eventLogId = grainReference.GetPrimaryKey(out var tenantIdAsString);
        _executionContextManager.Establish(Guid.Parse(tenantIdAsString), string.Empty);
        var eventLogState = grainState.State as EventSequenceState;
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventLogId);
        return Collection.UpdateOneAsync(
            filter,
            Builders<EventSequenceState>.Update.Set(_ => _.SequenceNumber, eventLogState!.SequenceNumber),
            new() { IsUpsert = true });
    }
}

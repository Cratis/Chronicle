// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
public class ObserverStorageProvider : IGrainStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IEventStoreDatabase _eventStoreDatabase;

    IMongoCollection<ObserverState> Collection => _eventStoreDatabase.GetCollection<ObserverState>(CollectionNames.Observers);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabase">Provider for <see cref="IMongoDatabase"/>.</param>
    public ObserverStorageProvider(
        IExecutionContextManager executionContextManager,
        IEventStoreDatabase eventStoreDatabase)
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
        var observerId = grainReference.GetPrimaryKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString);
        var eventSequenceId = observerKey.EventSequenceId;
        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var key = GetKeyFrom(eventSequenceId, observerId);
        var cursor = await Collection.FindAsync(_ => _.Id == key);
        grainState.State = await cursor.FirstOrDefaultAsync() ?? new ObserverState
        {
            Id = key,
            EventSequenceId = eventSequenceId,
            ObserverId = observerId,
            Offset = EventSequenceNumber.First,
            LastHandled = EventSequenceNumber.First
        };
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var observerId = grainReference.GetPrimaryKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString);
        var eventSequenceId = observerKey.EventSequenceId;
        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var observerState = grainState.State as ObserverState;
        var key = GetKeyFrom(eventSequenceId, observerId);

        await Collection.ReplaceOneAsync(
            _ => _.Id == key,
            observerState!,
            new ReplaceOptions { IsUpsert = true });
    }

    string GetKeyFrom(EventSequenceId eventSequenceId, ObserverId observerId) => $"{eventSequenceId} : {observerId}";
}

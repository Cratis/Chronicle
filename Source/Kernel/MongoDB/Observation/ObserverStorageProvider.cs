// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
public class ObserverStorageProvider : IGrainStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    /// <summary>
    /// Gets the <ze cref="IMongoCollection{TDocument}"/> for <see cref="ObserverState"/>.
    /// </summary>
    protected IMongoCollection<ObserverState> Collection => _eventStoreDatabaseProvider().GetCollection<ObserverState>(CollectionNames.Observers);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public ObserverStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _executionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var observerId = grainReference.GetPrimaryKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString);
        var eventSequenceId = observerKey.EventSequenceId;

        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var key = GetKeyFrom(observerKey, observerId);
        var cursor = await Collection.FindAsync(_ => _.Id == key);
        grainState.State = await cursor.FirstOrDefaultAsync() ?? new ObserverState
        {
            Id = key,
            EventSequenceId = eventSequenceId,
            ObserverId = observerId,
            NextEventSequenceNumber = EventSequenceNumber.First,
            LastHandled = EventSequenceNumber.First,
            RunningState = ObserverRunningState.New
        };
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var observerId = grainReference.GetPrimaryKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString);
        var eventSequenceId = observerKey.EventSequenceId;
        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var observerState = grainState.State as ObserverState;
        var key = GetKeyFrom(observerKey, observerId);

        await Collection.ReplaceOneAsync(
            _ => _.Id == key,
            observerState!,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <summary>
    /// Get the key for the observer state.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to get for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <returns>The actual key.</returns>
    protected string GetKeyFrom(ObserverKey key, ObserverId observerId) => key.SourceMicroserviceId is not null ?
        $"{key.EventSequenceId} : {observerId} : {key.SourceMicroserviceId}" :
        $"{key.EventSequenceId} : {observerId}";
}

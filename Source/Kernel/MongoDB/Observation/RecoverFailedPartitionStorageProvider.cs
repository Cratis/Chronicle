// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using MongoDB.Driver;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling <see cref="RecoverFailedPartitionState" /> storage.
/// </summary>
public class RecoverFailedPartitionStorageProvider : IGrainStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    /// <summary>
    /// Gets the <see cref="IExecutionContextManager"/> for working with the execution context.
    /// </summary>
    protected IExecutionContextManager ExecutionContextManager { get; }

    /// <summary>
    /// Gets the <ze cref="IMongoCollection{TDocument}"/> for <see cref="RecoverFailedPartitionState"/>.
    /// </summary>
    protected IMongoCollection<RecoverFailedPartitionState> Collection => _eventStoreDatabaseProvider().GetCollection<RecoverFailedPartitionState>(CollectionNames.FailedPartitions);

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoverFailedPartitionStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public RecoverFailedPartitionStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        ExecutionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var partitionedObserverKey = PartitionedObserverKey.Parse(observerKeyAsString!);

        ExecutionContextManager.Establish(partitionedObserverKey.TenantId, CorrelationId.New(), partitionedObserverKey.MicroserviceId);

        var key = GetKeyFrom(partitionedObserverKey, observerId);
        await Collection.DeleteOneAsync(_ => _.Id == key).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<RecoverFailedPartitionState>)!;
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var partitionedObserverKey = PartitionedObserverKey.Parse(observerKeyAsString!);

        ExecutionContextManager.Establish(partitionedObserverKey.TenantId, CorrelationId.New(), partitionedObserverKey.MicroserviceId);

        var key = GetKeyFrom(partitionedObserverKey, observerId);
        var cursor = await Collection.FindAsync(_ => _.Id == key).ConfigureAwait(false);

        actualGrainState.State = await cursor.FirstOrDefaultAsync().ConfigureAwait(false) ?? new RecoverFailedPartitionState()
        {
            Id = key,
            ObserverId = observerId
        };
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var observerId = grainId.GetGuidKey(out var keyAsString);
        var partitionKey = PartitionedObserverKey.Parse(keyAsString!);
        ExecutionContextManager.Establish(partitionKey.TenantId, CorrelationId.New(), partitionKey.MicroserviceId);

        var observerState = (grainState.State as RecoverFailedPartitionState)!;
        var key = GetKeyFrom(partitionKey, observerId);
        observerState.Id = key;

        if (!observerState.HasBeenInitialized())
        {
            await Collection.DeleteOneAsync(_ => _.Id == key).ConfigureAwait(false);
        }
        else
        {
            await Collection.ReplaceOneAsync(
                _ => _.Id == key,
                observerState!,
                new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get the key for the observer state.
    /// </summary>
    /// <param name="key"><see cref="PartitionedObserverKey"/> to get for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <returns>The actual key.</returns>
    protected string GetKeyFrom(PartitionedObserverKey key, ObserverId observerId) => $"{observerId} : {key.EventSequenceId} : {key.EventSourceId}";
}

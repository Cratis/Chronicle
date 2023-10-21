// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using MongoDB.Driver;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
public class ObserverStorageProvider : IGrainStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    public ObserverStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider)
    {
        ExecutionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
    }

    /// <summary>
    /// Gets the <see cref="IExecutionContextManager"/> for working with the execution context.
    /// </summary>
    protected IExecutionContextManager ExecutionContextManager { get; }

    /// <summary>
    /// Gets the <ze cref="IMongoCollection{TDocument}"/> for <see cref="ObserverState"/>.
    /// </summary>
    protected IMongoCollection<ObserverState> Collection => _eventStoreDatabaseProvider().GetCollection<ObserverState>(CollectionNames.Observers);

    /// <summary>
    /// Gets the <ze cref="IMongoCollection{TDocument}"/> for <see cref="RecoverFailedPartitionState"/>.
    /// </summary>
    protected IMongoCollection<RecoverFailedPartitionState> RecoverFailedPartitionCollection => _eventStoreDatabaseProvider().GetCollection<RecoverFailedPartitionState>(CollectionNames.FailedPartitions);

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var currentSubscription = actualGrainState.State.CurrentSubscription;
        var observerId = (ObserverId)grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);
        var eventSequenceId = observerKey.EventSequenceId;

        ExecutionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var failedPartitionsCursor = await RecoverFailedPartitionCollection.FindAsync(_ => _.ObserverId == observerId).ConfigureAwait(false);
        var failedPartitions = failedPartitionsCursor.ToList().Select(_ => new FailedPartition(
            _.Partition,
            _.CurrentError,
            _.Messages,
            _.StackTrace,
            _.InitialPartitionFailedOn)).ToArray();

        var key = GetKeyFrom(observerKey, observerId);

        var cursor = await Collection.FindAsync(_ => _.Id == key).ConfigureAwait(false);
        var loadedState = cursor.FirstOrDefault();

        var state = loadedState ?? new ObserverState
        {
            Id = key,
            EventSequenceId = eventSequenceId,
            ObserverId = observerId,
            NextEventSequenceNumber = EventSequenceNumber.First,
            LastHandled = EventSequenceNumber.First,
            RunningState = ObserverRunningState.New
        };
        state.FailedPartitions = failedPartitions;
        state.CurrentSubscriptionType = actualGrainState.State?.CurrentSubscriptionType;
        state.CurrentSubscriptionArguments = actualGrainState.State?.CurrentSubscriptionArguments;
        actualGrainState.State = state;

        if (currentSubscription.EventTypes.Any())
        {
            actualGrainState.State.TailEventSequenceNumber = await _eventSequenceStorageProvider().GetTailSequenceNumber(eventSequenceId);
            if (actualGrainState.State.NextEventSequenceNumber < actualGrainState.State.TailEventSequenceNumber)
            {
                actualGrainState.State.NextEventSequenceNumberForEventTypes = await _eventSequenceStorageProvider().GetNextSequenceNumberGreaterOrEqualThan(
                    eventSequenceId,
                    actualGrainState.State.NextEventSequenceNumber,
                    actualGrainState.State.CurrentSubscription.EventTypes);
            }
        }
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);
        var eventSequenceId = observerKey.EventSequenceId;
        ExecutionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var observerState = grainState.State as ObserverState;
        var key = GetKeyFrom(observerKey, observerId);

        await Collection.ReplaceOneAsync(
            _ => _.Id == key,
            observerState!,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
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

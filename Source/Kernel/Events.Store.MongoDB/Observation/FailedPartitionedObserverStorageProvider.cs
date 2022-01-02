// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Observation;
using Cratis.Execution;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Events.Store.MongoDB.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
    /// </summary>
    public class FailedPartitionedObserverStorageProvider : IGrainStorage
    {
        const string CollectionName = "failed-observers";
        readonly IExecutionContextManager _executionContextManager;
        readonly IEventStoreDatabase _eventStoreDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObserverStorageProvider"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
        /// <param name="eventStoreDatabase">Provider for <see cref="IMongoDatabase"/>.</param>
        public FailedPartitionedObserverStorageProvider(
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
            var observerId = grainReference.GetPrimaryKey(out var key);
            var (tenantId, eventLogId, eventSourceId) = PartitionedObserverKeyHelper.Parse(key);
            _executionContextManager.Establish(tenantId, string.Empty);

            var filter = GetFilterFor(eventLogId, observerId, eventSourceId);
            var cursor = await Collection.FindAsync(filter);
            grainState.State = await cursor.FirstOrDefaultAsync() ?? new FailedPartitionedObserverState();
        }

        /// <inheritdoc/>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var observerId = grainReference.GetPrimaryKey(out var key);
            var (tenantId, eventLogId, eventSourceId) = PartitionedObserverKeyHelper.Parse(key);
            _executionContextManager.Establish(tenantId, string.Empty);

            var filter = GetFilterFor(eventLogId, observerId, eventSourceId);

            var state = (grainState.State as FailedPartitionedObserverState)!;
            if (state.IsFailed)
            {
                await Collection.ReplaceOneAsync(
                    filter,
                    state!,
                    new ReplaceOptions { IsUpsert = true });
            }
            else
            {
                await Collection.FindOneAndDeleteAsync(filter);
            }
        }

        FilterDefinition<FailedPartitionedObserverState> GetFilterFor(Guid eventLogId, Guid observerId, string? eventSourceId)
        {
            var key = $"{eventLogId}+{observerId}+{eventSourceId}";
            return Builders<FailedPartitionedObserverState>.Filter.Eq(
                new StringFieldDefinition<FailedPartitionedObserverState, string>("_id"), key
            );
        }

        IMongoCollection<FailedPartitionedObserverState> Collection => _eventStoreDatabase.GetCollection<FailedPartitionedObserverState>(CollectionName);
    }
}

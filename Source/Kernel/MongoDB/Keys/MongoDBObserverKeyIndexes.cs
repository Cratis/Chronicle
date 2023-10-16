// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class MongoDBObserverKeyIndexes : IObserverKeyIndexes
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverKeyIndexes"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public MongoDBObserverKeyIndexes(
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        IExecutionContextManager executionContextManager)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task<IObserverKeyIndex> GetFor(
        MicroserviceId microserviceId,
        TenantId tenantId,
        ObserverId observerId,
        EventSequenceNumber fromEventSequenceNumber)
    {
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var database = _eventStoreDatabaseProvider();
        var collection = database.GetEventSequenceCollectionFor(EventSequenceId.Log);
        await Task.CompletedTask;
        return new MongoDBEventSourceKeyIndex(collection);
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IObserverStorage"/> for MongoDB.
/// </summary>
public class MongoDBObserverStorage : IObserverStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverStorage"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public MongoDBObserverStorage(ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverId>> GetObserversForEventTypes(IEnumerable<EventType> eventTypes)
    {
        return Task.FromResult(Collection
            .Find(_ => true)
            .ToEnumerable()
            .Where(_ => _.EventTypes.Any(_ => eventTypes.Contains(_)))
            .Select(_ => _.ObserverId).ToArray().AsEnumerable());
    }

    IMongoCollection<ObserverState> Collection => _eventStoreDatabaseProvider().GetObserverStateCollection();
}

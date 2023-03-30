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
    public Task<ObserverInformation> GetObserver(ObserverId observerId) =>
        Task.FromResult(ToObserverInformation(Collection
            .Find(_ => _.ObserverId == observerId)
            .First()));

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverInformation>> GetObserversForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(_ => _.Id).ToArray();
        return Task.FromResult(Collection
            .Find(_ => true)
            .ToEnumerable()
            .Where(observer => observer.EventTypes.Any(_ => eventTypeIds.Contains(_.Id)))
            .Select(_ => ToObserverInformation(_)).ToArray().AsEnumerable());
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverInformation>> GetAllObservers() =>
        Task.FromResult(Collection
            .Find(_ => true)
            .ToEnumerable()
            .Select(_ => ToObserverInformation(_)).ToArray().AsEnumerable());

    ObserverInformation ToObserverInformation(ObserverState state) => new(
        state.ObserverId,
        state.EventSequenceId,
        state.Name,
        state.Type,
        state.EventTypes,
        state.NextEventSequenceNumber,
        state.RunningState);

    IMongoCollection<ObserverState> Collection => _eventStoreDatabaseProvider().GetObserverStateCollection();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IObserverStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IEventStoreNamespaceDatabase"/>.</param>
public class ObserverStorage(IEventStoreNamespaceDatabase database) : IObserverStorage
{
    IMongoCollection<ObserverState> Collection => database.GetObserverStateCollection();

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformation>> ObserveAll()
    {
        var observerInformation = GetAll().GetAwaiter().GetResult();
        return Collection.Observe(observerInformation, HandleChangesForObservers);
    }

    /// <inheritdoc/>
    public Task<ObserverInformation> Get(ObserverId observerId) =>
        Collection
            .Aggregate()
            .Match(_ => _.ObserverId == observerId)
            .JoinWithFailedPartitions()
            .FirstAsync();

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(_ => _.Id).ToArray();
        return await Collection
            .Aggregate()
            .Match(_ => _.EventTypes.Any(_ => eventTypeIds.Contains(_.Id)))
            .JoinWithFailedPartitions()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetAll()
    {
        var aggregation = Collection.Aggregate().JoinWithFailedPartitions();
        var cursor = await aggregation.ToCursorAsync();
        return cursor.ToList();
    }

    /// <inheritdoc/>
    public async Task<ObserverState> GetState(ObserverId observerId, ObserverKey observerKey)
    {
        var filter = GetKeyFilter(observerId, observerKey);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        return await cursor.FirstOrDefaultAsync().ConfigureAwait(false) ?? new ObserverState(
            [],
            observerKey.EventSequenceId,
            observerId,
            ObserverName.NotSpecified,
            ObserverType.Unknown,
            EventSequenceNumber.First,
            EventSequenceNumber.First,
            EventCount.NotSet,
            ObserverRunningState.New);
    }

    /// <inheritdoc/>
    public async Task SaveState(ObserverId observerId, ObserverKey observerKey, ObserverState state)
    {
        var filter = GetKeyFilter(observerId, observerKey);
        await Collection.ReplaceOneAsync(
            filter,
            state!,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    void HandleChangesForObservers(IChangeStreamCursor<ChangeStreamDocument<ObserverState>> cursor, List<ObserverInformation> observers)
    {
        foreach (var changedObserver in cursor.Current.Select(_ => _.FullDocument))
        {
            var observerInformation = ToObserverInformation(changedObserver);
            var observer = observers.Find(_ => _.ObserverId == changedObserver.ObserverId);
            if (observer is not null)
            {
                var index = observers.IndexOf(observer);
                observers[index] = observerInformation;
            }
            else
            {
                observers.Add(observerInformation);
            }
        }
    }

    ObserverInformation ToObserverInformation(ObserverState state) => new(
        state.ObserverId,
        state.EventSequenceId,
        state.Name,
        state.Type,
        state.EventTypes,
        state.NextEventSequenceNumber,
        state.LastHandledEventSequenceNumber,
        state.RunningState,
        state.Handled,
        []);

    string GetKeyFrom(ObserverId observerId, ObserverKey key) => $"{key.EventSequenceId} : {observerId}";

    FilterDefinition<ObserverState> GetKeyFilter(ObserverId observerId, ObserverKey key) =>
        Builders<ObserverState>.Filter.Eq(new StringFieldDefinition<ObserverState, string>("_id"), GetKeyFrom(observerId, key));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Reactive;
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
    public ISubject<IEnumerable<ObserverInformation>> ObserveAll()
    {
        var collectionSubject = Collection.Observe();
        return new TransformingSubject<IEnumerable<ObserverState>, IEnumerable<ObserverInformation>>(
            collectionSubject,
            observers => observers.Select(_ => ToObserverInformation(_)).ToArray());
    }

    /// <inheritdoc/>
    public Task<ObserverInformation> Get(ObserverId observerId) =>
        Collection
            .Aggregate()
            .Match(_ => _.Id == observerId)
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
    public async Task<ObserverState> GetState(ObserverKey observerKey)
    {
        var filter = GetKeyFilter(observerKey.ObserverId);
        using var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        return await cursor.FirstOrDefaultAsync().ConfigureAwait(false) ?? new ObserverState(
            observerKey.ObserverId,
            [],
            observerKey.EventSequenceId,
            ObserverType.Unknown,
            EventSequenceNumber.Unavailable,
            ObserverRunningState.Unknown,
            new HashSet<Key>(),
            new HashSet<Key>(),
            false);
    }

    /// <inheritdoc/>
    public async Task SaveState(ObserverKey observerKey, ObserverState state)
    {
        var filter = GetKeyFilter(observerKey.ObserverId);
        await Collection.ReplaceOneAsync(
            filter,
            state!,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    static ObserverInformation ToObserverInformation(ObserverState state) => new(
        state.Id,
        state.EventSequenceId,
        state.Type,
        state.EventTypes,
        state.NextEventSequenceNumber,
        state.LastHandledEventSequenceNumber,
        state.RunningState,
        []);

    static FilterDefinition<ObserverState> GetKeyFilter(ObserverId observerId) =>
        Builders<ObserverState>.Filter.Eq(new StringFieldDefinition<ObserverState, string>("_id"), observerId);
}

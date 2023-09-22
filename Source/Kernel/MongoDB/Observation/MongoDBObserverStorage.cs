// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IObserverStorage"/> for MongoDB.
/// </summary>
public class MongoDBObserverStorage : IObserverStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ILogger<MongoDBObserverStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverStorage"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public MongoDBObserverStorage(
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ILogger<MongoDBObserverStorage> logger)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverState>> All
    {
        get
        {
            var observers = Collection.Find(_ => true).ToList();
            var observable = new BehaviorSubject<IEnumerable<ObserverState>>(observers);
            var filter = Builders<ChangeStreamDocument<ObserverState>>.Filter.In(
                new StringFieldDefinition<ChangeStreamDocument<ObserverState>, string>("operationType"),
                new[] { "insert", "replace", "update" });

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<ObserverState>>().Match(filter);

            _ = Task.Run(async () =>
            {
                try
                {
                    var cursor = await Collection.WatchAsync(
                        pipeline,
                        new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });

                    while (await cursor.MoveNextAsync())
                    {
                        if (observable.IsDisposed)
                        {
                            cursor.Dispose();
                            return;
                        }

                        if (!cursor.Current.Any()) continue;
                        if (!observable.IsDisposed)
                        {
                            foreach (var changedObserver in cursor.Current.Select(_ => _.FullDocument))
                            {
                                var observer = observers.Find(_ => _.Id == changedObserver.Id);
                                if (observer is not null)
                                {
                                    var index = observers.IndexOf(observer);
                                    observers[index] = changedObserver;
                                }
                                else
                                {
                                    observers.Add(changedObserver);
                                }
                            }

                            observable.OnNext(observers);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.WatchConnectionLost(ex);
                }
            });

            return observable;
        }
    }

    IMongoCollection<ObserverState> Collection => _eventStoreDatabaseProvider().GetObserverStateCollection();

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

    /// <inheritdoc/>
    public async Task<ObserverState> GetState(ObserverId observerId, ObserverKey observerKey)
    {
        var key = GetKeyFrom(observerKey, observerId);
        var cursor = await Collection.FindAsync(_ => _.Id == key).ConfigureAwait(false);
        return await cursor.FirstOrDefaultAsync().ConfigureAwait(false) ?? new ObserverState
        {
            Id = key,
            EventSequenceId = observerKey.EventSequenceId,
            ObserverId = observerId,
            NextEventSequenceNumber = EventSequenceNumber.First,
            LastHandledEventSequenceNumber = EventSequenceNumber.First,
            RunningState = ObserverRunningState.New
        };
    }

    /// <inheritdoc/>
    public async Task SaveState(ObserverId observerId, ObserverKey observerKey, ObserverState state)
    {
        var key = GetKeyFrom(observerKey, observerId);

        await Collection.ReplaceOneAsync(
            _ => _.Id == key,
            state!,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
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
        Enumerable.Empty<FailedPartition>());

    string GetKeyFrom(ObserverKey key, ObserverId observerId) => key.SourceMicroserviceId is not null ?
        $"{key.EventSequenceId} : {observerId} : {key.SourceMicroserviceId}" :
        $"{key.EventSequenceId} : {observerId}";
}

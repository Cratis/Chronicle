// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserversState"/> for MongoDB.
/// </summary>
public class MongoDBObserversState : IObserversState
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ILogger<MongoDBObserversState> _logger;

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverState>> All
    {
        get
        {
            var observable = new BehaviorSubject<IEnumerable<ObserverState>>(new ObserverState[0]);
            var observers = Collection.Find(_ => true).ToList();
            observable.OnNext(observers);

            var filter = Builders<ChangeStreamDocument<ObserverState>>.Filter.In(
                new StringFieldDefinition<ChangeStreamDocument<ObserverState>, string>("operationType"),
                new[] { "insert", "replace", "update" });

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<ObserverState>>().Match(filter);

            var cursor = Collection.Watch(
                pipeline,
                new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });
            _ = Task.Run(async () =>
            {
                try
                {
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

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserversState"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public MongoDBObserversState(
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ILogger<MongoDBObserversState> logger)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _logger = logger;
    }

    IMongoCollection<ObserverState> Collection => _eventStoreDatabaseProvider().GetObserverStateCollection();
}

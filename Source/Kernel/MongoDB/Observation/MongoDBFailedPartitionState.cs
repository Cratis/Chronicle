// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsState"/> for MongoDB.
/// </summary>
public class MongoDBFailedPartitionState : IFailedPartitionsState
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ILogger<MongoDBObserversState> _logger;

    /// <inheritdoc/>
    public IObservable<IEnumerable<RecoverFailedPartitionState>> All
    {
        get
        {
            var observers = Collection.Find(_ => true).ToList();
            var observable = new BehaviorSubject<IEnumerable<RecoverFailedPartitionState>>(observers);
            observable.OnNext(observers);
            var cursor = Collection.Watch();
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
                        var observers = await Collection.FindAsync(_ => true);
                        if (!observable.IsDisposed)
                        {
                            observable.OnNext(observers.ToList());
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

    IMongoCollection<RecoverFailedPartitionState> Collection => _eventStoreDatabaseProvider().GetCollection<RecoverFailedPartitionState>(CollectionNames.FailedPartitions);

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserversState"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public MongoDBFailedPartitionState(
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ILogger<MongoDBObserversState> logger)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _logger = logger;
    }
}

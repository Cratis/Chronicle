// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitionsStorage"/> for MongoDB.
/// </summary>
public class MongoDBFailedPartitionStorage : IFailedPartitionsStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ILogger<MongoDBObserversState> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserversState"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public MongoDBFailedPartitionStorage(
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ILogger<MongoDBObserversState> logger)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<FailedPartition>> All
    {
        get
        {
            var observers = Collection.Find(_ => true).ToList();
            var observable = new BehaviorSubject<IEnumerable<FailedPartition>>(observers);
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
    IMongoCollection<FailedPartition> Collection => _eventStoreDatabaseProvider().GetCollection<FailedPartition>(CollectionNames.FailedPartitions);

    /// <inheritdoc/>
    public async Task Save(ObserverId observerId, FailedPartitions failedPartitions)
    {
        foreach (var failedPartition in failedPartitions.Partitions)
        {
            if (!failedPartition.IsResolved)
            {
                await Collection.DeleteOneAsync(_ => _.Id == failedPartition.Id).ConfigureAwait(false);
            }
            else
            {
                await Collection.ReplaceOneAsync(
                    _ => _.Id == failedPartition.Id,
                    failedPartition!,
                    new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<FailedPartitions> GetFor(ObserverId observerId)
    {
        var cursor = await Collection.FindAsync(_ => _.ObserverId == observerId).ConfigureAwait(false);

        return new FailedPartitions
        {
            Partitions = cursor.ToList()
        };
    }
}

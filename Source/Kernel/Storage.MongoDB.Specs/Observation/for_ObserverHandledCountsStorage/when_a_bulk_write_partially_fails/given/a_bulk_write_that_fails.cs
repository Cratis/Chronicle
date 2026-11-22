// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails.given;

public class a_bulk_write_that_fails : for_ObserverHandledCountsStorage.given.an_observer_handled_counts_storage
{
    protected readonly ConnectionId _connectionId = new(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
    protected Func<WriteModel<ObserverPartitionCounts>[], Exception> _failure;
    protected Exception _driverError;
    protected Exception _error;
    protected readonly KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>[] _counts =
    [
        new((Key)"leading", new Dictionary<EventTypeId, EventCount>()),
        new((Key)"first", new Dictionary<EventTypeId, EventCount> { ["event"] = 2UL }),
        new((Key)"interleaved", new Dictionary<EventTypeId, EventCount>()),
        new((Key)"second", new Dictionary<EventTypeId, EventCount> { ["event"] = 3UL }),
        new((Key)"third", new Dictionary<EventTypeId, EventCount> { ["event"] = 4UL }),
        new((Key)"trailing", new Dictionary<EventTypeId, EventCount>())
    ];

    void Establish() => _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<ObserverPartitionCounts>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
        .Returns(call =>
        {
            _driverError = _failure(call.Arg<IEnumerable<WriteModel<ObserverPartitionCounts>>>().ToArray());
            return Task.FromException<BulkWriteResult<ObserverPartitionCounts>>(_driverError);
        });

    protected MongoBulkWriteException<ObserverPartitionCounts> Rejected(WriteModel<ObserverPartitionCounts>[] models, int failedIndex)
    {
        var processed = models[..(failedIndex + 1)];
        var result = new BulkWriteResult<ObserverPartitionCounts>.Acknowledged(models.Length, 0, 0, 0, 0, processed, []);
        return new(_connectionId, result, [WriteError(failedIndex)], null, models[(failedIndex + 1)..]);
    }

    protected static BulkWriteError WriteError(int index) => Create<BulkWriteError>(index, ServerErrorCategory.DuplicateKey, 11000, "rejected", new BsonDocument());

    static T Create<T>(params object[] arguments) =>
        (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null)!;
}

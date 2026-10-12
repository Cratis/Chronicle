// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately.given;

public class a_controlled_mongo_sink(MongoDBFixture fixture) : a_bulk_sink(fixture)
{
    protected readonly TaskCompletionSource _batchStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _releaseBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly Exception _unavailable = new MongoWriteUnavailable();
    protected bool _holdFirstBatch;
    protected Exception? _firstBatchError;
    protected Exception? _directWriteError;
    protected Exception? _joinWriteError;
    protected Action<int>? _afterBatch;
    int _batchNumber;

    protected override IMongoCollection<BsonDocument> InterceptCollection(IMongoCollection<BsonDocument> collection)
    {
        var intercepted = Substitute.For<IMongoCollection<BsonDocument>>();
        intercepted.CollectionNamespace.Returns(collection.CollectionNamespace);
        intercepted.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(info => WriteBatch(collection, info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray()));
        intercepted.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(info => _directWriteError is { } error
                ? Task.FromException<UpdateResult>(error)
                : collection.UpdateOneAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<UpdateDefinition<BsonDocument>>(1), info.ArgAt<UpdateOptions>(2)));
        intercepted.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(info => _joinWriteError is { } error
                ? Task.FromException<UpdateResult>(error)
                : collection.UpdateManyAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<UpdateDefinition<BsonDocument>>(1), info.ArgAt<UpdateOptions>(2)));
        intercepted.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(info => collection.FindAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<FindOptions<BsonDocument, BsonDocument>>(1)));
        intercepted.DeleteOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(info => collection.DeleteOneAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0)));
        return intercepted;
    }

    protected async Task QueueBelowThreshold()
    {
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
        for (var index = 2; index < 1000; index++)
        {
            await _sink.ApplyChanges(new Key($"root-{index}", ArrayIndexers.NoIndexers), Changes(index), (ulong)index);
        }
    }

    protected Task<IEnumerable<FailedPartition>> ReachThreshold() => _sink.ApplyChanges(new Key("root-1000", ArrayIndexers.NoIndexers), Changes(1000), 1000UL);

    protected static IChangeset<AppendedEvent, ExpandoObject> Removal()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([]);
        changeset.HasBeenRemoved().Returns(true);
        return changeset;
    }

    async Task<BulkWriteResult<BsonDocument>> WriteBatch(IMongoCollection<BsonDocument> collection, WriteModel<BsonDocument>[] operations)
    {
        var batchNumber = Interlocked.Increment(ref _batchNumber);
        if (batchNumber == 1)
        {
            _batchStarted.SetResult();
            if (_holdFirstBatch)
            {
                await _releaseBatch.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }

            if (_firstBatchError is { } error)
            {
                throw error;
            }
        }

        var result = await collection.BulkWriteAsync(operations);
        _afterBatch?.Invoke(batchNumber);
        return result;
    }

    sealed class MongoWriteUnavailable() : Exception("The MongoDB write outcome is unknown");
}

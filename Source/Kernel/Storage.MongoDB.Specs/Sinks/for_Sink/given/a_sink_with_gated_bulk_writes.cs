// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Reflection;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public class a_sink_with_gated_bulk_writes : Specification
{
    protected Sink _sink;
    protected readonly TaskCompletionSource _firstFlushStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _releaseFirstFlush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly List<WriteModel<BsonDocument>[]> _batches = [];
    protected readonly List<UpdateOneModel<BsonDocument>> _directWrites = [];
    protected readonly Key _firstKey = new("first", ArrayIndexers.NoIndexers);
    protected readonly Key _secondKey = new("second", ArrayIndexers.NoIndexers);
    protected bool _failLaterBatch;
    protected bool _failFirstBatch;
    protected bool _throwFirstBatch;
    protected bool _throwLaterBatch;
    protected readonly Exception _unexpectedFailure = new BulkServerUnavailable();
    protected bool _holdFirstFlush = true;
    protected IMongoCollection<BsonDocument> _collection;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            "bulk-model",
            "BulkModel",
            "bulk",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, JsonSchema.FromType<Model>() } },
            []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.CollectionNamespace.Returns(new CollectionNamespace(new DatabaseNamespace("specs"), "bulk"));
        var collections = new FixedCollections(_collection, new SinkCollections(readModel, new MongoClient().GetDatabase("specs")));
        _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando, new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance));
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(info => WriteBatch(info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray()));
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _directWrites.Add(new UpdateOneModel<BsonDocument>(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<UpdateDefinition<BsonDocument>>(1)));
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
            });
    }

    protected Task<IEnumerable<FailedPartition>> Apply(Key key, int count, ulong sequenceNumber) =>
        _sink.ApplyChanges(key, Changes(count), sequenceNumber);

    protected static Changeset<AppendedEvent, ExpandoObject> Changes(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.ReplaceState(state, [new PropertyDifference("count", null, count)]);
        return changeset;
    }

    protected int WrittenCountFor(Key key) => _batches.SelectMany(batch => batch).OfType<UpdateOneModel<BsonDocument>>()
        .Concat(_directWrites)
        .Count(operation => operation.Filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry))["_id"] == key.Value.ToString());

    protected static Changeset<AppendedEvent, ExpandoObject> JoinedChanges(int count)
    {
        var changeset = Changes(count);
        changeset.Add(new Joined(new ExpandoObject(), "other", "id", ArrayIndexers.NoIndexers, []) { HasKeyedFrom = true });
        return changeset;
    }

    protected int[] WrittenValuesFor(Key key) => _batches.SelectMany(batch => batch).OfType<UpdateOneModel<BsonDocument>>()
        .Concat(_directWrites)
        .Where(operation => operation.Filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry))["_id"] == key.Value.ToString())
        .Select(operation => operation.Update.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry))["$set"]["count"].AsInt32)
        .ToArray();

    async Task<BulkWriteResult<BsonDocument>> WriteBatch(WriteModel<BsonDocument>[] operations)
    {
        _batches.Add(operations);
        if (_batches.Count == 1 && _holdFirstFlush)
        {
            _firstFlushStarted.SetResult();
            await _releaseFirstFlush.Task;
        }

        if ((_batches.Count == 1 && _throwFirstBatch) || (_batches.Count > 1 && _throwLaterBatch))
        {
            throw _unexpectedFailure;
        }

        var result = new BulkWriteResult<BsonDocument>.Acknowledged(operations.Length, 0, 0, 0, 0, operations, []);
        if ((_batches.Count == 1 && _failFirstBatch) || (_batches.Count > 1 && _failLaterBatch))
        {
            var error = (BulkWriteError)Activator.CreateInstance(typeof(BulkWriteError), BindingFlags.Instance | BindingFlags.NonPublic, null, [0, ServerErrorCategory.DuplicateKey, 11000, "duplicate key", new BsonDocument()], null)!;
            var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
            var failedResult = new BulkWriteResult<BsonDocument>.Acknowledged(operations.Length, 0, 0, 0, 0, operations[..1], []);
            throw new MongoBulkWriteException<BsonDocument>(connection, failedResult, [error], null, operations[1..]);
        }

        return result;
    }

    record Model(string Id, int Count);

    sealed class BulkServerUnavailable() : Exception("The server did not answer");

    sealed class FixedCollections(IMongoCollection<BsonDocument> collection, SinkCollections inner) : ISinkCollections
    {
        public string PromotingCollectionName => inner.PromotingCollectionName;
        public IMongoCollection<BsonDocument> GetCollection() => collection;
        public IMongoCollection<BsonDocument> GetCollection(string collectionName) => collection;
        public Task BeginReplay(ReplayContext context) => inner.BeginReplay(context);
        public Task ResumeReplay(ReplayContext context) => inner.ResumeReplay(context);
        public Task EndReplay(ReplayContext context) => inner.EndReplay(context);
        public void AbandonReplay() => inner.AbandonReplay();
        public Task PrepareInitialRun() => inner.PrepareInitialRun();
        public Task Remove(ReadModelContainerName collectionName) => inner.Remove(collectionName);
    }
}

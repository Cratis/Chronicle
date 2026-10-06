// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

/// <summary>
/// A sink over a substituted collection whose first bulk write is held until the spec releases it, so a spec can act
/// while the flush is in flight. Every operation the sink sends - in a bulk batch or directly - is recorded in order.
/// </summary>
public class a_sink_with_gated_bulk_writes : Specification
{
    protected Sink _sink;
    protected IMongoCollection<BsonDocument> _collection;
    protected readonly TaskCompletionSource _firstFlushStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _releaseFirstFlush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly List<WriteModel<BsonDocument>[]> _batches = [];
    protected readonly List<UpdateOneModel<BsonDocument>> _directWrites = [];
    protected readonly List<UpdateOneModel<BsonDocument>> _written = [];
    protected readonly Key _firstKey = new("first", ArrayIndexers.NoIndexers);
    protected readonly Key _secondKey = new("second", ArrayIndexers.NoIndexers);
    protected readonly Exception _unexpectedFailure = new BulkServerUnavailable();
    protected bool _holdFirstFlush = true;
    protected bool _throwFirstBatch;

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
                var write = new UpdateOneModel<BsonDocument>(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<UpdateDefinition<BsonDocument>>(1));
                _directWrites.Add(write);
                _written.Add(write);
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

    /// <summary>
    /// Gets the count values written for a key, in the order they reached the collection.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> to get the written values for.</param>
    /// <returns>The written values.</returns>
    protected int[] WrittenValuesFor(Key key) => _written
        .Where(operation => Render(operation.Filter)["_id"] == key.Value.ToString())
        .Select(operation => Render(operation.Update)["$set"]["count"].AsInt32)
        .ToArray();

    static BsonDocument Render(FilterDefinition<BsonDocument> filter) => filter.Render(RenderArgs);

    static BsonDocument Render(UpdateDefinition<BsonDocument> update) => update.Render(RenderArgs).AsBsonDocument;

    static RenderArgs<BsonDocument> RenderArgs => new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);

    async Task<BulkWriteResult<BsonDocument>> WriteBatch(WriteModel<BsonDocument>[] operations)
    {
        _batches.Add(operations);
        if (_batches.Count == 1 && _holdFirstFlush)
        {
            _firstFlushStarted.SetResult();
            await _releaseFirstFlush.Task;
        }

        if (_batches.Count == 1 && _throwFirstBatch)
        {
            throw _unexpectedFailure;
        }

        _written.AddRange(operations.OfType<UpdateOneModel<BsonDocument>>());
        return new BulkWriteResult<BsonDocument>.Acknowledged(operations.Length, 0, 0, 0, 0, operations, []);
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

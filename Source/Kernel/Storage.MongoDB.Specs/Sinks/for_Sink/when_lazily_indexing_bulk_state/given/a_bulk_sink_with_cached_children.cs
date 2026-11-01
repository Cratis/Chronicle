// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state.given;

public class a_bulk_sink_with_cached_children : Specification
{
    protected Sink _sink;
    protected IMongoCollection<BsonDocument> _collection;
    protected BsonDocument[] _readDocuments = [];
    protected bool _failWrites;
    protected readonly Key _first = new("first", ArrayIndexers.NoIndexers);
    protected readonly Key _second = new("second", ArrayIndexers.NoIndexers);
    ulong _sequence;

    protected object? ChildIndex => typeof(Sink).GetField("_bulkChildIndex", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_sink);

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"count":{"type":"integer"},"children":{"type":"array","items":{"type":"object","properties":{"childId":{"type":"string"}}}}}}
            """);
        var definition = new ReadModelDefinition("model", "model", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, definition, NullLogger<MongoDBConverter>.Instance);
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.CollectionNamespace.Returns(new CollectionNamespace(new DatabaseNamespace("specs"), "model"));
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(_collection);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Cursor(_readDocuments)));
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(info => _failWrites
                ? Task.FromException<BulkWriteResult<BsonDocument>>(new InvalidOperationException("Detached write failed"))
                : Task.FromResult<BulkWriteResult<BsonDocument>>(new BulkWriteResult<BsonDocument>.Acknowledged(1, 0, 0, 0, 0, info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray(), [])));
        _sink = new Sink(definition, converter, collections, new ChangesetConverter(definition, converter, collections, expando), expando, Substitute.For<IReadModelChangeStreams>());
        await _sink.BeginBulk();
    }

    void Destroy() => _sink.Dispose();

    protected static ExpandoObject Root(params string[] children) => WithChildren(children.Select(Child).Cast<object>().ToList());

    protected static ExpandoObject WithChildren(IEnumerable<object> children)
    {
        var state = new ExpandoObject();
        var values = (IDictionary<string, object?>)state;
        values["count"] = 1;
        values["children"] = children;
        return state;
    }

    protected static ExpandoObject Child(string id)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["childId"] = id;
        return child;
    }

    protected async Task Apply(Key key, ExpandoObject state)
    {
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changes.ReplaceState(state, [new PropertyDifference("count", null, 1)]);
        await _sink.ApplyChanges(key, changes, new EventSequenceNumber(++_sequence));
    }

    protected async Task Remove(Key key)
    {
        var state = new ExpandoObject();
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, state);
        changes.Add(new Removed(state));
        await _sink.ApplyChanges(key, changes, new EventSequenceNumber(++_sequence));
    }

    protected async Task<Key?> Find(string value)
    {
        var result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), value);
        return result.TryGetValue(out var key) ? key : null;
    }

    static IAsyncCursor<BsonDocument> Cursor(BsonDocument[] documents)
    {
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns(documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(documents.Length > 0), Task.FromResult(false));
        return cursor;
    }
}

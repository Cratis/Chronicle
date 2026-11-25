// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order.given;

public class a_bulk_sink_recording_writes : a_sink_recording_writes
{
    protected readonly List<string> _orderedWrites = [];
    protected readonly List<WriteModel<BsonDocument>[]> _batches = [];
    protected Key RootKey => new(_key, ArrayIndexers.NoIndexers);

    void Establish()
    {
        _collection.When(collection => collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>()))
            .Do(call => RecordOperation("direct", call.Arg<UpdateDefinition<BsonDocument>>()));
        _collection.When(collection => collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>()))
            .Do(call => RecordOperation("join", call.Arg<UpdateDefinition<BsonDocument>>()));
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => WriteBatch(call.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray()));
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => ReadDocument());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<BsonDocument>(), Arg.Any<JsonSchema>())
            .Returns(call => State(call.Arg<BsonDocument>()["value"].AsInt32));
    }

    protected async Task<ExpandoObject?> SeedAndReadCachedState()
    {
        await _sink.BeginBulk();
        var seed = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        seed.Changes.Returns([Direct(5)]);
        seed.CurrentState.Returns(State(5));
        await _sink.ApplyChanges(RootKey, seed, (EventSequenceNumber)1UL);
        await _sink.FlushBulk();
        return await _sink.FindOrDefault(RootKey);
    }

    protected async Task ApplyGuarded()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([Direct(10), Join(20)]);
        changeset.HasJoined().Returns(true);
        changeset.CurrentState.Returns(State(20));
        await _sink.ApplyChanges(RootKey, changeset, (EventSequenceNumber)42UL, SinkWriteMode.GuardedUpsert);
    }

    protected static int ValueOf(ExpandoObject state) => (int)((IDictionary<string, object?>)state)["value"]!;

    static ExpandoObject State(int value)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["value"] = value;
        return state;
    }

    void RecordOperation(string kind, UpdateDefinition<BsonDocument> update)
    {
        var rendered = update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
        if (rendered.Contains("$unset"))
        {
            _orderedWrites.Add("repair");
        }
        if (rendered.TryGetValue("$set", out var properties))
        {
            _orderedWrites.Add($"{kind}:{properties["value"]}");
        }
    }

    BulkWriteResult<BsonDocument> WriteBatch(WriteModel<BsonDocument>[] operations)
    {
        _batches.Add(operations);
        foreach (var operation in operations.Cast<UpdateOneModel<BsonDocument>>())
        {
            RecordOperation("direct", operation.Update);
            var rendered = operation.Update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
            if (rendered.TryGetValue("$set", out var properties))
            {
                _documents[_key]["value"] = properties["value"];
            }
        }
        return new BulkWriteResult<BsonDocument>.Acknowledged(operations.Length, 0, 0, operations.Length, 0, operations, []);
    }

    IAsyncCursor<BsonDocument> ReadDocument()
    {
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([_documents[_key]]);
        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        return cursor;
    }
}

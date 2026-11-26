// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order.given;

public class a_sink_recording_writes : for_ChangesetConverter.given.a_changeset_converter
{
    protected readonly List<string> _writes = [];
    protected readonly Dictionary<string, BsonDocument> _documents = new()
    {
        ["root"] = new() { ["joinId"] = "shared", ["value"] = 0 },
        ["joined-only"] = new() { ["joinId"] = "shared", ["value"] = 0 },
        ["direct-only"] = new() { ["joinId"] = "other", ["value"] = 0 }
    };
    protected string _key = "root";
    protected Change[] _changes;
    protected Sink _sink;

    void Establish()
    {
        _mongoDBConverter.ToBsonValue(Arg.Any<Key>()).Returns(call => new BsonString(call.Arg<Key>().Value.ToString()));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(new BsonInt64(42));
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(call.Arg<PropertyPath>().ToString(), []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => BsonValue.Create(call.ArgAt<object>(0)));
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Record("direct", call, [_documents[_key]]));
        _collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Record("join", call, _documents.Values.Where(document => document["joinId"] == "shared")));
        _sink = new Sink(_readModel, _mongoDBConverter, _sinkCollections, _converter, _expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
    }

    protected async Task Apply()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(_changes);
        changeset.HasJoined().Returns(_changes.Any(change => change is Joined));
        await _sink.ApplyChanges(new Key(_key, ArrayIndexers.NoIndexers), changeset, (EventSequenceNumber)42UL);
    }

    protected static Change Direct(int value) => new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("value"), 0, value)]);

    protected static Change Join(int value) => new Joined(new ExpandoObject(), "shared", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(value)]) { HasKeyedFrom = true };

    UpdateResult Record(string kind, NSubstitute.Core.CallInfo call, IEnumerable<BsonDocument> documents)
    {
        var update = call.Arg<UpdateDefinition<BsonDocument>>().Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
        if (update.Contains("$pull"))
        {
            _writes.Add("remove");
        }
        else if (update.TryGetValue("$set", out var properties))
        {
            _writes.Add($"{kind}:{properties["value"]}");
            foreach (var document in documents)
            {
                document["value"] = properties["value"];
            }
        }
        return new UpdateResult.Acknowledged(1, 1, null);
    }
}

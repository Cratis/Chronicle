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
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

public class a_sink_over_a_substituted_collection : Specification
{
    protected record Write(string Kind, BsonDocument Update);

    protected List<Write> _writes;
    protected SinkWriteMode _mode;
    protected Change[] _changes;
    Sink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;

    void Establish()
    {
        _writes = [];
        _mode = SinkWriteMode.Always;
        var readModel = new ReadModelDefinition(
            "test",
            "test",
            "test",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromType<TestReadModel>()
            },
            []);
        var mongoConverter = Substitute.For<IMongoDBConverter>();
        var collections = Substitute.For<ISinkCollections>();
        var expandoConverter = Substitute.For<IExpandoObjectConverter>();
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collections.GetCollection().Returns(collection);
        mongoConverter.ToBsonValue(Arg.Any<Key>()).Returns(new BsonString("root"));
        mongoConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(call.Arg<PropertyPath>().Path, []));
        mongoConverter.ToBsonValue(Arg.Any<object>()).Returns(new BsonInt64(20));
        mongoConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => BsonValue.Create(call.ArgAt<object?>(0)));
        var converter = new ChangesetConverter(readModel, mongoConverter, collections, expandoConverter);
        _sink = new Sink(readModel, mongoConverter, collections, converter, expandoConverter, Substitute.For<IReadModelChangeStreams>());

        collection.UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Do<UpdateDefinition<BsonDocument>>(update => _writes.Add(new("one", Render(update)))),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null)));
        collection.UpdateManyAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Do<UpdateDefinition<BsonDocument>>(update => _writes.Add(new("many", Render(update)))),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null)));
    }

    protected static PropertiesChanged<ExpandoObject> Direct(params (string Property, long Value)[] values) =>
        new(new ExpandoObject(), values.Select(_ => new PropertyDifference(new PropertyPath(_.Property), 0L, _.Value)).ToArray());

    protected static Joined Join(params (string Property, long Value)[] values) =>
        new(new ExpandoObject(), "root", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(values)])
        {
            HasKeyedFrom = true
        };

    protected async Task Apply()
    {
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns(_changes);
        _changeset.HasJoined().Returns(true);
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), _changeset, 20UL, _mode);
    }

    static BsonDocument Render(UpdateDefinition<BsonDocument> update) =>
        update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;

    record TestReadModel(string Id, long Count, long X, string JoinId);
}

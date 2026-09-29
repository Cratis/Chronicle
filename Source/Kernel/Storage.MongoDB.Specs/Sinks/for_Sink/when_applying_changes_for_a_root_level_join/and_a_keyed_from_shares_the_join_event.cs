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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

public class and_a_keyed_from_shares_the_join_event : Specification
{
    IMongoCollection<BsonDocument> _collection;
    Sink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    FilterDefinition<BsonDocument> _filter;
    UpdateDefinition<BsonDocument> _update;
    UpdateOptions _options;

    void Establish()
    {
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
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collections.GetCollection().Returns(_collection);
        mongoConverter.ToBsonValue(Arg.Any<Key>()).Returns(new BsonString("root"));
        mongoConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(new MongoDBProperty("count", []));
        mongoConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>()).Returns(new BsonInt64(1));
        var converter = new ChangesetConverter(readModel, mongoConverter, collections, expandoConverter);
        _sink = new Sink(readModel, mongoConverter, collections, converter, expandoConverter, Substitute.For<IReadModelChangeStreams>());

        var change = new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [new PropertyDifference(new PropertyPath("count"), 0L, 1L)]);
        var joined = new Joined(new ExpandoObject(), "other-key", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [])
        {
            HasKeyedFrom = true
        };
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([change, joined]);
        _changeset.HasJoined().Returns(true);
        _collection.UpdateOneAsync(
            Arg.Do<FilterDefinition<BsonDocument>>(filter => _filter = filter),
            Arg.Do<UpdateDefinition<BsonDocument>>(update => _update = update),
            Arg.Do<UpdateOptions>(options => _options = options),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null)));
    }

    async Task Because() => await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.Unavailable);

    [Fact] void should_write_the_from_change_to_the_root_key() => _filter.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry))["_id"].AsString.ShouldEqual("root");
    [Fact] void should_persist_the_count_increment() => _update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry))["$set"]["count"].AsInt64.ShouldEqual(1);
    [Fact] void should_allow_the_from_to_construct_the_root() => _options.IsUpsert.ShouldBeTrue();

    record TestReadModel(string Id, long Count, string JoinId);
}

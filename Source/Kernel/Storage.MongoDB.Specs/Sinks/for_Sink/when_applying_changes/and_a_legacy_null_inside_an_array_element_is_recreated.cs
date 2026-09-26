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
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_a_legacy_null_inside_an_array_element_is_recreated.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_a_legacy_null_inside_an_array_element_is_recreated(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        readonly string _databaseName = $"chronicle_sink_specs_{Guid.NewGuid():N}";
        IMongoClient _client = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        Sink _sink = default!;

        public BsonDocument Direct = default!;
        public BsonDocument Bulk = default!;
        public BsonDocument Nested = default!;
        public BsonDocument NestedArrays = default!;
        public BsonDocument JoinedNull = default!;
        public BsonDocument JoinedExisting = default!;

        public async Task InitializeAsync()
        {
            _client = new MongoClient(fixture.ConnectionString);
            var database = _client.GetDatabase(_databaseName);
            var schema = await JsonSchema.FromJsonAsync("""
                {"type":"object","properties":{"id":{"type":"string"},"joinKey":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"info":{"type":"object","properties":{"name":{"type":"string"}}},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"info":{"type":"object","properties":{"name":{"type":"string"}}}}}}}}},"outer":{"type":"object","properties":{"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"info":{"type":"object","properties":{"name":{"type":"string"}}}}}}}}}}
                """);
            var readModel = new ReadModelDefinition("array-model", "ArrayModel", $"array_{Guid.NewGuid():N}", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
            var formats = new TypeFormats();
            var expando = new ExpandoObjectConverter(formats);
            var collections = new SinkCollections(readModel, database);
            var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
            _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando);
            _collection = collections.GetCollection();

            await Recreate("direct", "[items].info.name", "[items]");
            await _sink.BeginBulk();
            await Recreate("bulk", "[items].info.name", "[items]");
            await _sink.EndBulk();
            await Recreate("nested", "outer.[items].info.name", "outer.[items]");
            await RecreateNestedArrays();
            await RecreateJoinedArray();
            Direct = await Find("direct");
            Bulk = await Find("bulk");
            Nested = await Find("nested");
            NestedArrays = await Find("nested-arrays");
            JoinedNull = await Find("joined-null");
            JoinedExisting = await Find("joined-existing");
        }

        public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

        async Task Recreate(string id, PropertyPath leaf, PropertyPath array)
        {
            var items = new BsonArray
            {
                new BsonDocument { { "_id", "first" }, { "info", BsonNull.Value } },
                new BsonDocument { { "_id", "second" }, { "info", new BsonDocument("name", "Keep") } }
            };
            var document = new BsonDocument("_id", id);
            if (array.Path == "[items]")
            {
                document["items"] = items;
            }
            else
            {
                document["outer"] = new BsonDocument("items", items);
            }

            await _collection.InsertOneAsync(document);
            var key = new Key(id, ArrayIndexers.NoIndexers);
            var initial = await _sink.FindOrDefault(key);
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(initial);
            changeset.CurrentState.Returns(initial);
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(leaf, null, "Again", new ArrayIndexers([new ArrayIndexer(array, "id", "first")]))])]);
            await _sink.ApplyChanges(key, changeset, 1UL);
        }

        async Task RecreateNestedArrays()
        {
            await _collection.InsertOneAsync(new BsonDocument
            {
                { "_id", "nested-arrays" },
                {
                    "items",
                    new BsonArray
                    {
                        Item("first",
                            new BsonDocument { { "_id", "child-1" }, { "info", BsonNull.Value } },
                            new BsonDocument { { "_id", "child-2" }, { "info", new BsonDocument("name", "Keep") } }),
                        Item("second", new BsonDocument { { "_id", "child-1" }, { "info", BsonNull.Value } })
                    }
                }
            });
            var key = new Key("nested-arrays", ArrayIndexers.NoIndexers);
            var initial = await _sink.FindOrDefault(key);
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(initial);
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [
                new PropertyDifference("[items].[children].info.name", null, "Again", new ArrayIndexers(
                [
                    new ArrayIndexer("[items]", "id", "first"),
                    new ArrayIndexer("[items].[children]", "id", "child-1")
                ]))
            ])]);
            await _sink.ApplyChanges(key, changeset, 1UL);
        }

        async Task RecreateJoinedArray()
        {
            await _collection.InsertManyAsync(
            [
                new BsonDocument
                {
                    { "_id", "joined-null" },
                    { "joinKey", "common" },
                    { "items", new BsonArray { new BsonDocument { { "_id", "first" }, { "info", BsonNull.Value } } } }
                },
                new BsonDocument
                {
                    { "_id", "joined-existing" },
                    { "joinKey", "common" },
                    { "items", new BsonArray { new BsonDocument { { "_id", "first" }, { "info", new BsonDocument("name", "Keep") } } } }
                }
            ]);
            var indexers = new ArrayIndexers([new ArrayIndexer("[items]", "id", "first")]);
            var joined = new Joined(
                new ExpandoObject(),
                "common",
                "joinKey",
                ArrayIndexers.NoIndexers,
                [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[items].info.name", null, "Joined", indexers)])]);
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.Changes.Returns([joined]);
            await _sink.ApplyChanges(new Key("common", ArrayIndexers.NoIndexers), changeset, 1UL);
        }

        static BsonDocument Item(string id, params BsonDocument[] children) => new()
        {
            { "_id", id },
            { "children", new BsonArray(children) }
        };

        async Task<BsonDocument> Find(string id) => await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();
    }

    [Fact] void should_recreate_only_the_matching_child() => ctx.Direct["items"][0]["info"]["name"].AsString.ShouldEqual("Again");
    [Fact] void should_leave_the_sibling_untouched() => ctx.Direct["items"][1]["info"]["name"].AsString.ShouldEqual("Keep");
    [Fact] void should_recreate_the_child_inside_a_nested_object() => ctx.Nested["outer"]["items"][0]["info"]["name"].AsString.ShouldEqual("Again");
    [Fact] void should_leave_the_nested_sibling_untouched() => ctx.Nested["outer"]["items"][1]["info"]["name"].AsString.ShouldEqual("Keep");
    [Fact] void should_recreate_the_child_in_an_ordered_bulk() => ctx.Bulk["items"][0]["info"]["name"].AsString.ShouldEqual("Again");
    [Fact] void should_leave_the_bulk_sibling_untouched() => ctx.Bulk["items"][1]["info"]["name"].AsString.ShouldEqual("Keep");
    [Fact] void should_recreate_the_inner_array_child() => ctx.NestedArrays["items"][0]["children"][0]["info"]["name"].AsString.ShouldEqual("Again");
    [Fact] void should_leave_the_inner_array_sibling_untouched() => ctx.NestedArrays["items"][0]["children"][1]["info"]["name"].AsString.ShouldEqual("Keep");
    [Fact] void should_leave_the_outer_array_sibling_untouched() => ctx.NestedArrays["items"][1]["children"][0]["info"].IsBsonNull.ShouldBeTrue();
    [Fact] void should_recreate_a_legacy_null_for_each_joined_document() => ctx.JoinedNull["items"][0]["info"]["name"].AsString.ShouldEqual("Joined");
    [Fact] void should_update_an_existing_joined_child() => ctx.JoinedExisting["items"][0]["info"]["name"].AsString.ShouldEqual("Joined");
}

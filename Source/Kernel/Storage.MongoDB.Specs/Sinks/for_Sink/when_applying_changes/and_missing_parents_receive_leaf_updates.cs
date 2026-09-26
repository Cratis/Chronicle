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
using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_missing_parents_receive_leaf_updates.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_missing_parents_receive_leaf_updates(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        IMongoClient _client = default!;
        string _databaseName = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        Sink _sink = default!;

        public BsonDocument LegacyResult = default!;
        public BsonDocument NullOuterResult = default!;
        public BsonDocument NewResult = default!;
        public BsonDocument PiiResult = default!;
        public BsonDocument JoinedNullResult = default!;
        public BsonDocument JoinedExistingResult = default!;

        public async Task InitializeAsync()
        {
            _databaseName = $"chronicle_sink_specs_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            var database = _client.GetDatabase(_databaseName);
            var schema = await JsonSchema.FromJsonAsync("""
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string" },
                    "joinKey": { "type": "string" },
                    "outer": {
                      "type": "object",
                      "properties": {
                        "title": { "type": "string" },
                        "info": {
                          "type": "object",
                          "properties": {
                            "name": { "type": "string" },
                            "other": { "type": "string" }
                          }
                        }
                      }
                    }
                  }
                }
                """);
            var readModel = new ReadModelDefinition(
                "nested-model",
                "NestedModel",
                $"nested_{Guid.NewGuid():N}",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Projection,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
                []);
            var formats = new TypeFormats();
            var expando = new ExpandoObjectConverter(formats);
            var collections = new SinkCollections(readModel, database);
            var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
            _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando);
            _collection = collections.GetCollection();

            await _collection.InsertOneAsync(new BsonDocument { { "_id", "legacy" }, { "outer", new BsonDocument { { "info", BsonNull.Value } } } });
            var legacyKey = new Key("legacy", ArrayIndexers.NoIndexers);
            var legacyInitial = (await _sink.FindOrDefault(legacyKey))!;
            await _sink.ApplyChanges(
                legacyKey,
                Changes(legacyInitial, new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("outer.info.name", null, "Again")])),
                1UL);
            LegacyResult = await Find("legacy");

            await _collection.InsertOneAsync(new BsonDocument { { "_id", "null-outer" }, { "outer", BsonNull.Value } });
            var nullOuterKey = new Key("null-outer", ArrayIndexers.NoIndexers);
            var nullOuterInitial = (await _sink.FindOrDefault(nullOuterKey))!;
            await _sink.ApplyChanges(
                nullOuterKey,
                Changes(nullOuterInitial, new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("outer.info.name", null, "From null")])),
                1UL);
            NullOuterResult = await Find("null-outer");

            var newKey = new Key("new", ArrayIndexers.NoIndexers);
            await _sink.ApplyChanges(
                newKey,
                Changes(
                    new ExpandoObject(),
                    new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("outer.title", null, "Title")]),
                    new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("outer.info.name", null, "Inner")])),
                1UL);
            NewResult = await Find("new");

            var piiKey = new Key("pii", ArrayIndexers.NoIndexers);

            // Encryption changes only the leaf difference; State still holds plaintext.
            dynamic plaintext = new ExpandoObject();
            plaintext.outer = new ExpandoObject();
            plaintext.outer.info = new ExpandoObject();
            plaintext.outer.info.name = "Ada Lovelace";
            await _sink.ApplyChanges(
                piiKey,
                Changes(
                    new ExpandoObject(),
                    new PropertiesChanged<ExpandoObject>((ExpandoObject)plaintext, [new PropertyDifference("outer.info.name", null, "encrypted-display-name")])),
                1UL);
            PiiResult = await Find("pii");

            await _collection.InsertManyAsync([
                new BsonDocument { { "_id", "join-null" }, { "joinKey", "common" }, { "outer", BsonNull.Value } },
                new BsonDocument
                {
                    { "_id", "join-existing" },
                    { "joinKey", "common" },
                    { "outer", new BsonDocument { { "info", new BsonDocument { { "other", "Keep" } } } } }
                }
            ]);
            var joined = new Joined(
                new ExpandoObject(),
                "common",
                "joinKey",
                ArrayIndexers.NoIndexers,
                [new PropertiesChanged<ExpandoObject>(null!, [new PropertyDifference("outer.info.name", null, "Joined")])]);
            await _sink.ApplyChanges(new Key("common", ArrayIndexers.NoIndexers), Changes(new ExpandoObject(), joined), 1UL);
            JoinedNullResult = await Find("join-null");
            JoinedExistingResult = await Find("join-existing");
        }

        public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

        async Task<BsonDocument> Find(string id) => await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();

        static IChangeset<AppendedEvent, ExpandoObject> Changes(ExpandoObject initial, params Change[] changes)
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(initial);
            changeset.Changes.Returns(changes);
            return changeset;
        }
    }

    [Fact] void should_recreate_a_leaf_under_a_legacy_null() => ctx.LegacyResult["outer"]["info"]["name"].AsString.ShouldEqual("Again");
    [Fact] void should_unset_outermost_before_inner() => ctx.NullOuterResult["outer"]["info"]["name"].AsString.ShouldEqual("From null");
    [Fact] void should_retain_the_first_change_when_a_second_change_sets_an_inner_leaf() => ctx.NewResult["outer"]["title"].AsString.ShouldEqual("Title");
    [Fact] void should_apply_the_second_change_under_a_missing_parent() => ctx.NewResult["outer"]["info"]["name"].AsString.ShouldEqual("Inner");
    [Fact] void should_store_only_ciphertext_under_a_missing_parent() => ctx.PiiResult["outer"]["info"]["name"].AsString.ShouldEqual("encrypted-display-name");
    [Fact] void should_repair_a_null_parent_per_joined_document() => ctx.JoinedNullResult["outer"]["info"]["name"].AsString.ShouldEqual("Joined");
    [Fact] void should_update_an_existing_joined_parent_without_replacing_its_siblings() => ctx.JoinedExistingResult["outer"]["info"]["name"].AsString.ShouldEqual("Joined");
    [Fact] void should_preserve_other_properties_on_the_existing_joined_parent() => ctx.JoinedExistingResult["outer"]["info"]["other"].AsString.ShouldEqual("Keep");
}

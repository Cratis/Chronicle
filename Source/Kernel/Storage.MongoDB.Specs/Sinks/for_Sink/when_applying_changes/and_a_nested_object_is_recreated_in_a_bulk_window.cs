// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_a_nested_object_is_recreated_in_a_bulk_window.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_a_nested_object_is_recreated_in_a_bulk_window(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        readonly string _databaseName = MongoDBSpecDatabaseNames.New();
        readonly ObjectComparer _comparer = new();
        IMongoClient _client = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        Sink _sink = default!;

        public BsonDocument Identical = default!;
        public BsonDocument PartlyChanged = default!;
        public bool CachedStateWasCleared = true;

        public async Task InitializeAsync()
        {
            _client = new MongoClient(fixture.ConnectionString);
            var database = _client.GetDatabase(_databaseName);
            var schema = await JsonSchema.FromJsonAsync("""
                {"type":"object","properties":{"id":{"type":"string"},"info":{"type":"object","properties":{"name":{"type":"string"},"description":{"type":"string"}}}}}
                """);
            var readModel = new ReadModelDefinition("nested-model", "NestedModel", $"nested_{Guid.NewGuid():N}", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
            var formats = new TypeFormats();
            var expando = new ExpandoObjectConverter(formats);
            var collections = new SinkCollections(readModel, database);
            var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
            _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando, Substitute.For<IReadModelChangeStreams>());
            _collection = collections.GetCollection();

            await _sink.BeginBulk();
            await Recreate("identical", "Before");
            await Recreate("partly-changed", "After");
            await _sink.EndBulk();
            Identical = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "identical")).SingleAsync();
            PartlyChanged = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "partly-changed")).SingleAsync();
        }

        public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

        async Task Recreate(string id, string recreatedName)
        {
            var key = new Key(id, ArrayIndexers.NoIndexers);
            var create = await Changeset(key);
            SetInfo(create, "Before", "Keep");
            await _sink.ApplyChanges(key, create, 0UL);

            var clear = await Changeset(key);
            clear.ClearNested("info", ArrayIndexers.NoIndexers);
            await _sink.ApplyChanges(key, clear, 1UL);
            var state = await _sink.FindOrDefault(key);
            CachedStateWasCleared &= state is not null &&
                ((IDictionary<string, object?>)state).TryGetValue("info", out var info) && info is null;

            var recreate = await Changeset(key);
            SetInfo(recreate, recreatedName, "Keep");
            await _sink.ApplyChanges(key, recreate, 2UL);
        }

        async Task<Changeset<AppendedEvent, ExpandoObject>> Changeset(Key key) =>
            new(_comparer, null!, await _sink.FindOrDefault(key) ?? new ExpandoObject());

        static void SetInfo(Changeset<AppendedEvent, ExpandoObject> changeset, string name, string description) =>
            changeset.SetProperties(
                [
                    PropertyMappers.FromEventValueProvider("info.name", _ => name),
                    PropertyMappers.FromEventValueProvider("info.description", _ => description)
                ],
                ArrayIndexers.NoIndexers);
    }

    [Fact] void should_recreate_the_identical_name() => ctx.Identical["info"]["name"].AsString.ShouldEqual("Before");
    [Fact] void should_recreate_the_identical_description() => ctx.Identical["info"]["description"].AsString.ShouldEqual("Keep");
    [Fact] void should_recreate_the_changed_name() => ctx.PartlyChanged["info"]["name"].AsString.ShouldEqual("After");
    [Fact] void should_recreate_the_unchanged_description() => ctx.PartlyChanged["info"]["description"].AsString.ShouldEqual("Keep");
    [Fact] void should_cache_the_cleared_state_between_events() => ctx.CachedStateWasCleared.ShouldBeTrue();
}

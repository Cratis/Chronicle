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
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

[PerFactSpecificationLifecycle]
public class when_materializing_flat_keys(MongoDBFixture fixture) : Specification, IClassFixture<MongoDBFixture>
{
    MongoSinkHarness _harness;
    ISink _sink;
    IMongoCollection<BsonDocument> _collection;

    void Establish()
    {
        var model = new ReadModelDefinition(
            "order",
            "orders",
            "Order",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"customerId":{"type":"string"},"orderNumber":{"type":"integer","format":"int32"},"amount":{"type":"integer","format":"int32"}}}""")
            },
            []);
        _harness = new MongoSinkHarness { Fixture = fixture };
        _sink = _harness.CreateSink(model);
        _collection = new SinkCollections(model, _harness.Database).GetCollection();
    }

    void Destroy() => _harness.Dispose();

    [Fact]
    public async Task should_create_update_and_recreate_without_an_id_property()
    {
        var key = Key(1);
        await Write(key, 10, 1);
        await Write(key, 20, 2);
        var document = await _collection.Find(new BsonDocument("customerId", "customer")).SingleAsync();
        document["orderNumber"].AsInt32.ShouldEqual(1);
        document["amount"].AsInt32.ShouldEqual(20);
        document.Contains("id").ShouldBeFalse();
        await Remove(key, 3);
        (await _sink.FindOrDefault(key)).ShouldBeNull();
        await Write(key, 30, 4);
        (await _collection.CountDocumentsAsync(new BsonDocument())).ShouldEqual(1L);
    }

    [Fact]
    public async Task should_select_and_remove_only_the_complete_identity_document()
    {
        await Write(Key(1), 10, 1);
        await Write(Key(2), 20, 2);
        (await _sink.FindOrDefault(Key(1))).ShouldNotBeNull();
        await Remove(Key(3), 1);
        await Remove(Key(1), 2);
        (await _sink.FindOrDefault(Key(1))).ShouldBeNull();
        (await _sink.FindOrDefault(Key(2))).ShouldNotBeNull();
        (await _collection.CountDocumentsAsync(new BsonDocument())).ShouldEqual(1L);
    }

    [Fact]
    public async Task should_prefetch_flat_keys_by_identity_without_caching_another_tuple()
    {
        await Write(Key(1), 10, 1);
        await _sink.BeginBulk();
        await ((ISinkPagePrefetch)_sink).PrefetchPage([Key(1), Key(2)]);
        (await _sink.FindOrDefault(Key(1))).ShouldNotBeNull();
        (await _sink.FindOrDefault(Key(2))).ShouldBeNull();
        await _sink.EndBulk();
    }

    [Fact]
    public async Task should_ignore_root_tuple_matches_with_an_unrelated_identity()
    {
        await _collection.InsertOneAsync(new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "customerId", "customer" }, { "orderNumber", 1 }, { "amount", 10 } });
        (await _sink.FindOrDefault(Key(1))).ShouldBeNull();
        (await _sink.FindInPrimaryOrDefault(Key(1))).ShouldBeNull();
        await _sink.BeginBulk();
        await ((ISinkPagePrefetch)_sink).PrefetchPage([Key(1)]);
        (await _sink.FindOrDefault(Key(1))).ShouldBeNull();
        await _sink.EndBulk();
        await Remove(Key(1), 2);
        (await _collection.CountDocumentsAsync(new BsonDocument())).ShouldEqual(1L);
    }

    async Task Write(FlatCompositeKey key, int amount, ulong sequence)
    {
        var changes = Changes(await _sink.FindOrDefault(key) ?? new ExpandoObject());
        var mappers = key.Parts.Select(part => PropertyMappers.FromEventValueProvider(part.Key, _ => part.Value!))
            .Append(PropertyMappers.FromEventValueProvider("amount", _ => amount));
        changes.SetProperties(mappers, ArrayIndexers.NoIndexers);
        await _sink.ApplyChanges(key, changes, sequence);
    }

    async Task Remove(FlatCompositeKey key, ulong sequence)
    {
        var state = await _sink.FindOrDefault(key) ?? new ExpandoObject();
        var changes = Changes(state);
        changes.Add(new Removed(state));
        await _sink.ApplyChanges(key, changes, sequence);
    }

    static Changeset<AppendedEvent, ExpandoObject> Changes(ExpandoObject state) => new(new ObjectComparer(), AppendedEvent.Empty(), state);

    static FlatCompositeKey Key(int number)
    {
        var parts = new ExpandoObject();
        ((IDictionary<string, object?>)parts)["customerId"] = "customer";
        ((IDictionary<string, object?>)parts)["orderNumber"] = number;
        return new FlatCompositeKey(parts, ArrayIndexers.NoIndexers);
    }
}

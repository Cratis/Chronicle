// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key_in_mongodb;

[Collection(MongoDBCollection.Name)]
public class and_key_parts_have_typed_values(MongoDBFixture fixture) : given.a_mongo_composite_key_container
{
    readonly Guid _guid = Guid.Parse("8ddebfde-063e-4b83-b329-dba7dcaf86f8");
    readonly DateTimeOffset _date = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    Key _typedKey;
    CompositeKeyLookupResult _result;
    BsonDocument _document;
    protected override MongoSinkHarness CreateHarness() => _mongo = new() { Fixture = fixture };
    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"guid":{"type":"string","format":"guid"},"date":{"type":"string","format":"date-time-offset"},"number":{"type":"integer"}}},"count":{"type":"integer"}}}""")
        }
    };
    async Task Establish()
    {
        IDictionary<string, object?> parts = new ExpandoObject();
        parts["guid"] = _guid;
        parts["date"] = _date;
        parts["number"] = 42L;
        _typedKey = new Key((ExpandoObject)parts, ArrayIndexers.NoIndexers);
        await _sink.ApplyChanges(_typedKey, ChangesetSettingCountTo(42), 42UL);
        _document = await Collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
    }
    async Task Because() => _result = await ((ISinkCompositeKeyLookup)_sink).FindByCompositeKey(_typedKey);
    [Fact] void should_round_trip_the_guid() => _document["_id"]["guid"].AsGuid.ShouldEqual(_guid);
    [Fact] void should_round_trip_the_date_time() => _document["_id"]["date"].BsonType.ShouldEqual(BsonType.DateTime);
    [Fact] void should_round_trip_the_number() => _document["_id"]["number"].ToInt64().ShouldEqual(42L);
    [Fact] void should_find_the_typed_key() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_read_the_written_state() => Count(_result).ShouldEqual(42);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

[Collection(MongoDBCollection.Name)]
public class when_looking_up_a_composite_key_after_the_collection_was_found_to_have_no_other_part_order(MongoDBFixture fixture) : Specification
{
    MongoSinkHarness _harness;
    ISink _sink;
    int _recommendations;
    ExpandoObject? _found;
    ExpandoObject? _foundAgain;

    void Establish()
    {
        _harness = new MongoSinkHarness { Fixture = fixture, RecommendReplayForLegacyCompositeKeys = Recommend };

        // The schema order (second, first) differs from the ordinal order (first, second) the legacy document was written in.
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"second":{"type":"string"},"first":{"type":"string"}}},"name":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("composite", "composite_keys", "Composite", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "composite-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    async Task Because()
    {
        _found = await _sink.FindOrDefault(KeyOf("a", "b"));

        // Decided once per sink: a document written in another order after the first miss is not probed for again.
        await _harness.Database.GetCollection<BsonDocument>("composite_keys").InsertOneAsync(new BsonDocument { { "_id", new BsonDocument { { "first", "a" }, { "second", "b" } } }, { "name", "legacy" } });
        _foundAgain = await _sink.FindOrDefault(KeyOf("a", "b"));
    }

    Task Recommend()
    {
        _recommendations++;
        return Task.CompletedTask;
    }

    void Destroy() => _harness.Dispose();

    static Key KeyOf(string first, string second)
    {
        var parts = (IDictionary<string, object?>)new ExpandoObject();
        parts["first"] = first;
        parts["second"] = second;
        return new Key((ExpandoObject)parts, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_not_find_the_key_on_the_first_lookup() => _found.ShouldBeNull();
    [Fact] void should_not_probe_again_on_the_second_lookup() => _foundAgain.ShouldBeNull();
    [Fact] void should_not_recommend_a_replay() => _recommendations.ShouldEqual(0);
}

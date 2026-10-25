// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_paging_instances;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_legacy_roots_have_non_boolean_markers(ReplicaSetMongoDBFixture fixture) : Contract.a_populated_sink<MongoSinkHarness>
{
    MongoSinkHarness _harness;
    ReadModelInstances _result;
    string[] _observed;

    protected override MongoSinkHarness CreateHarness() => _harness = new() { ConnectionString = fixture.ConnectionString };

    async Task Establish()
    {
        var documents = new[]
        {
            Root("legacy-null", BsonNull.Value),
            Root("legacy-string", "false"),
            Root("legacy-zero", 0),
            Root("legacy-array", new BsonArray { false }),
            Root("legacy-nested-array", new BsonArray { new BsonArray { false } }),
            Root("initialized", true),
            new BsonDocument { { "_id", "legacy-missing" }, { "name", "legacy-missing" } },
            Root("placeholder", false)
        };
        await _harness.Database.GetCollection<BsonDocument>("paged_read_models").InsertManyAsync(documents);
    }

    async Task Because()
    {
        _result = await _sink.GetInstances(take: 100);
        _observed = Names(await _sink.ObserveInstances(take: 100).FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact] void should_count_legacy_roots_but_not_the_placeholder() => _result.TotalCount.ShouldEqual(11L);
    [Fact] void should_page_all_visible_roots() => Names(_result).Length.ShouldEqual(11);
    [Fact] void should_not_page_the_placeholder() => Names(_result).ShouldNotContain("placeholder");
    [Fact] void should_return_the_initialized_root() => Names(_result).ShouldContain("initialized");
    [Fact] void should_return_a_legacy_root_without_a_marker() => Names(_result).ShouldContain("legacy-missing");
    [Fact] void should_not_treat_a_null_marker_as_false() => Names(_result).ShouldContain("legacy-null");
    [Fact] void should_not_treat_a_string_marker_as_false() => Names(_result).ShouldContain("legacy-string");
    [Fact] void should_not_treat_zero_as_false() => Names(_result).ShouldContain("legacy-zero");
    [Fact] void should_hide_an_array_containing_false_like_false() => Names(_result).ShouldNotContain("legacy-array");
    [Fact] void should_not_treat_a_nested_array_as_false() => Names(_result).ShouldContain("legacy-nested-array");
    [Fact] void should_observe_all_legacy_roots() => _observed.Length.ShouldEqual(11);
    [Fact] void should_not_observe_the_placeholder() => _observed.ShouldNotContain("placeholder");
    [Fact] void should_not_observe_an_array_containing_false() => _observed.ShouldNotContain("legacy-array");

    static BsonDocument Root(string name, BsonValue marker) => new()
    {
        { "_id", name },
        { "name", name },
        { WellKnownProperties.ReadModelInstanceInitialized, marker }
    };
}

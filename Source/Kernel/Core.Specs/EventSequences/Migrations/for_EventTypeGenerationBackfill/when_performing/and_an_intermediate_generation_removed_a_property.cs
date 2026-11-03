// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_an_intermediate_generation_removed_a_property : given.a_backfill
{
    const string Original = """{"id":"1","status":"legacy"}""";

    void Establish()
    {
        var full = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"status":{"type":["string","null"]}}}""");
        var intermediate = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        _definition = _definition with
        {
            Generations = [new(1, full), new(2, intermediate), new(3, full)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject { ["status"] = new JsonObject { ["$defaultValue"] = "pending" } }, new JsonObject())]
        };
        _snapshot = _snapshot with { Content = new Dictionary<EventTypeGeneration, string> { [1] = Original } };
    }

    async Task Because() => await Perform();

    [Fact] void should_add_only_the_missing_generations() => _additions.Select(addition => addition.Generation).ShouldEqual(new EventTypeGeneration[] { 2, 3 });
    [Fact] void should_default_the_reintroduced_property() => ((IDictionary<string, object?>)_additions.Single(addition => addition.Generation == 3).Content)["status"].ShouldEqual("pending");
    [Fact] void should_keep_the_appended_generation_bytes() => _writes[0].Content[1].ShouldEqual(Original);
    [Fact] void should_record_the_appended_source() => _additions.TrueForAll(addition => addition.Provenance.SourceIsAppended).ShouldBeTrue();
    [Fact] void should_record_the_migration_version() => _additions.TrueForAll(addition => addition.Provenance.MigrationsVersion == EventTypeMigrationsVersion.For(_definition.Migrations)).ShouldBeTrue();
    [Fact] void should_hash_each_added_generation() => _additions.TrueForAll(addition => addition.Hash == new EventHashCalculator().Calculate(TypeId, _snapshot.EventSourceId, addition.Content)).ShouldBeTrue();
}

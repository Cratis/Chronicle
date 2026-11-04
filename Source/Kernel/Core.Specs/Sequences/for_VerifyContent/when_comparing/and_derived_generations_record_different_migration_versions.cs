// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_derived_generations_record_different_migration_versions : given.a_generation_history
{
    void Establish()
    {
        var schema = _definition.Generations.Last().Schema;
        _definition = _definition with
        {
            Generations = [.. _definition.Generations, new(3, schema)],
            Migrations = [.. _definition.Migrations, new(2, 3, [], JsonNode.Parse("""{"detail":{"$mapValues":{"source":"detail","mappings":[{"from":"default","to":"third"}]}}}""")!.AsObject(), new())]
        };
        var version = EventTypeMigrationsVersion.For(_definition.Migrations);
        _versions[version] = _definition.Migrations.ToArray();
        _history = _history with
        {
            DerivedGenerations = new Dictionary<EventTypeGeneration, GenerationProvenance>(_history.DerivedGenerations) { [3] = new(2, false, version) }
        };
        _stored = _stored with
        {
            GenerationalContent = new Dictionary<int, string>(_stored.GenerationalContent) { [3] = """{"value":42,"detail":"third"}""" }
        };
        _storage.GetEventStore("store").EventTypes.HasFor("event", 3).Returns(true);
        _storage.GetEventStore("store").EventTypes.GetFor("event", 3).Returns(new EventTypeSchema(new("event", 3), EventTypeOwner.Client, EventTypeSource.Code, schema));
        ChangeMigrations();
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_select_each_generations_own_recorded_version() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_SeededEventTypesStorage;

public class when_stores_adopt_a_seed : Specification
{
    readonly KernelEvents.EventTypeId _id = new("seeded");
    SeededEventTypesStorage _first;
    SeededEventTypesStorage _second;
    SeededEventTypesStorage _later;
    KernelEvents.EventTypeDefinition _otherDefinition;
    KernelEvents.EventTypeDefinition _laterDefinition;
    bool _hasGeneration;
    int _generations;
    int _versions;

    async Task Because()
    {
        var generations = new[] { new KernelEvents.EventTypeGenerationDefinition(1, new JsonSchema()), new KernelEvents.EventTypeGenerationDefinition(2, new JsonSchema()) };
        var migration = new KernelEvents.EventTypeMigrationDefinition(1, 2, [], new JsonObject { ["upcast"] = "original" }, new JsonObject());
        var definition = new KernelEvents.EventTypeDefinition(_id, KernelEvents.EventTypeOwner.Client, false, generations, [migration]);
        var seed = new EventTypeSeed([new KernelEvents.EventTypeToRegister(definition, KernelEvents.EventTypeSource.Code, KernelEvents.EventTypeVisibility.Public, "origin")]);
        _first = Create(seed);
        _second = Create(seed);
        var local = await _first.GetDefinition(_id);
        local.Generations.First().Schema.Title = "changed";
        local.Migrations.First().UpcastJmesPath["upcast"] = "changed";
        _otherDefinition = await _second.GetDefinition(_id);
        _later = Create(seed);
        _laterDefinition = await _later.GetDefinition(_id);
        _hasGeneration = await _second.HasFor(_id, 2);
        _generations = (await _second.GetAllGenerationsForEventType(new(_id, 1))).Count();
        _versions = (await _second.GetMigrationsVersions(_id)).Count;
    }

    [Fact] void should_preserve_all_generations() => _generations.ShouldEqual(2);
    [Fact] void should_find_the_seeded_generation() => _hasGeneration.ShouldBeTrue();
    [Fact] void should_preserve_the_migration_version() => _versions.ShouldEqual(1);
    [Fact] void should_not_share_schema_mutations() => _otherDefinition.Generations.First().Schema.Title.ShouldNotEqual("changed");
    [Fact] void should_not_share_migration_mutations() => _otherDefinition.Migrations.First().UpcastJmesPath["upcast"]!.GetValue<string>().ShouldEqual("original");
    [Fact] void should_not_mutate_the_seed_for_later_stores() => _laterDefinition.Migrations.First().UpcastJmesPath["upcast"]!.GetValue<string>().ShouldEqual("original");

    static SeededEventTypesStorage Create(EventTypeSeed seed)
    {
        var storage = new SeededEventTypesStorage();
        storage.Seed(seed);
        return storage;
    }

    void Destroy()
    {
        _first?.Dispose();
        _second?.Dispose();
        _later?.Dispose();
    }
}

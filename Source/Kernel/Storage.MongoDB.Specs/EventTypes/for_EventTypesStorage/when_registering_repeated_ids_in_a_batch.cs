// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_repeated_ids_in_a_batch(MongoDBFixture fixture) : given.a_populated_migration_chain(fixture)
{
    EventTypeDefinition _cold;
    IEnumerable<EventTypeId> _changed;

    async Task Because()
    {
        var extension = _chain with { Generations = [new(4, new JsonSchema())], Migrations = [Bridge(3, 4, "'four'", "'three'")] };
        var edit = _stale with { Migrations = [Bridge(1, 2, "'edited'", "'one'")] };
        _changed = await _storage.Register([new(extension, EventTypeSource.Code), new(edit, EventTypeSource.Code)]);
        _cold = await ReadCold();
    }

    [Fact] void should_report_the_id_once() => _changed.ShouldContainOnly(_chain.Id);
    [Fact] void should_merge_all_four_schemas() => _cold.Generations.Select(_ => _.Generation.Value).ShouldContainOnly(1u, 2u, 3u, 4u);
    [Fact] void should_merge_all_three_bridges() => _cold.Migrations.Select(_ => _.FromGeneration.Value).ShouldContainOnly(1u, 2u, 3u);
    [Fact] void should_apply_the_explicit_edit() => _cold.Migrations.Single(_ => _.FromGeneration.Value == 1).UpcastJmesPath["value"].GetValue<string>().ShouldEqual("'edited'");
}

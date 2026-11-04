// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_a_migration_extension : given.a_populated_migration_chain
{
    EventTypeDefinition _cold;
    bool _changed;

    async Task Because()
    {
        _changed = await _storage.Register(_chain with { Generations = [new(4, new JsonSchema())], Migrations = [Bridge(3, 4, "'four'", "'three'")] });
        _cold = await ReadCold();
    }

    [Fact] void should_report_a_change() => _changed.ShouldBeTrue();
    [Fact] void should_keep_all_four_generations() => _cold.Generations.Select(_ => _.Generation.Value).ShouldContainOnly(1u, 2u, 3u, 4u);
    [Fact] void should_keep_all_three_bridges() => _cold.Migrations.Select(_ => _.FromGeneration.Value).ShouldContainOnly(1u, 2u, 3u);
    [Fact] void should_keep_the_original_bridges() => ShouldRetainBridges(_cold with { Migrations = _cold.Migrations.Where(_ => _.FromGeneration.Value < 3) });
}

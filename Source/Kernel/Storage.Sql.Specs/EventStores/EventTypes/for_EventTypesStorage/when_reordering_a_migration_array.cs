// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_reordering_a_migration_array : given.a_populated_migration_chain
{
    bool _changed;
    EventTypeDefinition _cold;

    async Task Establish()
    {
        _chain.Migrations.First().UpcastJmesPath["values"] = new JsonArray("first", "second");
        await _storage.Register(_chain);
    }

    async Task Because()
    {
        var upcast = _chain.Migrations.First().UpcastJmesPath.DeepClone().AsObject();
        upcast["values"] = new JsonArray("second", "first");
        _changed = await _storage.Register(_stale with { Migrations = [_chain.Migrations.First() with { UpcastJmesPath = upcast }] });
        _cold = await ReadCold();
    }

    [Fact] void should_report_the_payload_change() => _changed.ShouldBeTrue();
    [Fact] void should_keep_the_omitted_bridge() => _cold.Migrations.Count().ShouldEqual(2);
    [Fact] void should_store_the_new_array_order() => _cold.Migrations.First(_ => _.FromGeneration.Value == 1).UpcastJmesPath["values"][0].GetValue<string>().ShouldEqual("second");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_reordered_migration_json : given.a_populated_migration_chain
{
    EventType _after;
    bool _changed;

    async Task Establish()
    {
        var bridge = _chain.Migrations.First();
        bridge.UpcastJmesPath["nested"] = new JsonObject { ["first"] = "value", ["second"] = "value" };
        await _storage.Register(_chain);
        _before = await ReadRow();
    }

    async Task Because()
    {
        var bridge = _chain.Migrations.First() with { UpcastJmesPath = JsonNode.Parse("{\"nested\":{\"second\":\"value\",\"first\":\"value\"},\"two\":\"one\"}").AsObject() };
        _changed = await _storage.Register(_chain with { Migrations = [_chain.Migrations.Last(), bridge] });
        _after = await ReadRow();
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] void should_not_rewrite_equivalent_json() => _after.MigrationsJson.ShouldEqual(_before.MigrationsJson);
}

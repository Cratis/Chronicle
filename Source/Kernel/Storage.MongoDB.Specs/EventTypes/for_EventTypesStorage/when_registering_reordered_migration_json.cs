// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_reordered_migration_json(MongoDBFixture fixture) : given.a_populated_migration_chain(fixture)
{
    bool _changed;

    async Task Establish()
    {
        _chain.Migrations.First().UpcastJmesPath["nested"] = new JsonObject { ["first"] = "value", ["second"] = "value" };
        await _storage.Register(_chain);
    }

    async Task Because()
    {
        var bridge = _chain.Migrations.First() with { UpcastJmesPath = JsonNode.Parse("{\"nested\":{\"second\":\"value\",\"first\":\"value\"},\"two\":\"one\"}").AsObject() };
        _changed = await _storage.Register(_chain with { Migrations = [_chain.Migrations.Last(), bridge] });
    }

    [Fact] void should_not_report_a_change_for_object_or_endpoint_order() => _changed.ShouldBeFalse();
}

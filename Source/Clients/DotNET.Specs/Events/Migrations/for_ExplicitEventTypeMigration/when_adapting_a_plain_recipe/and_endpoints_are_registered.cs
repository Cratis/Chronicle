// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;

public class and_endpoints_are_registered : given.a_plain_migration
{
    JsonObject _upcast;
    JsonObject _downcast;

    void Because()
    {
        _migration = _methods.Create(_services, _eventTypes);
        var upcast = new EventMigrationBuilder();
        var downcast = new EventMigrationBuilder();
        _migration.Upcast(upcast);
        _migration.Downcast(downcast);
        _upcast = upcast.ToJson();
        _downcast = downcast.ToJson();
    }

    [Fact] void should_take_from_generation_from_the_owning_registry() => _migration.From.ShouldEqual((EventTypeGeneration)1);
    [Fact] void should_take_to_generation_from_the_owning_registry() => _migration.To.ShouldEqual((EventTypeGeneration)2);
    [Fact] void should_inject_the_owning_registry_instead_of_di_registry() => _activation.Received(1).Record(_eventTypes);
    [Fact] void should_use_the_existing_upcast_rename_expression() => _upcast["NewName"]!["$rename"]!.GetValue<string>().ShouldEqual("OldName");
    [Fact] void should_use_the_existing_downcast_rename_expression() => _downcast["OldName"]!["$rename"]!.GetValue<string>().ShouldEqual("NewName");
    [Fact] void should_apply_the_forward_value_map() => _upcast["Status"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("new");
    [Fact] void should_invert_the_value_map_on_downcast() => _downcast["Status"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("old");
}

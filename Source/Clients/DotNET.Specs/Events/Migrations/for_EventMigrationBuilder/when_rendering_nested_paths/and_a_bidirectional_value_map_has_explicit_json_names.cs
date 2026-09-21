// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_a_bidirectional_value_map_has_explicit_json_names : Specification
{
    EventValueMapBuilder<given.AttributedTarget, given.AttributedSource> _maps;
    EventMigrationBuilder _upcastBuilder;
    EventMigrationBuilder _downcastBuilder;
    JsonObject _upcast;
    JsonObject _downcast;

    void Establish()
    {
        _maps = new();
        _maps.For(target => target.Contact.Status, source => source.Contact.Status, map => map.Map("old", "new"));
        _upcastBuilder = new(new given.PrefixPolicy());
        _downcastBuilder = new(new given.PrefixPolicy());
    }

    void Because()
    {
        _upcastBuilder.Properties(_maps.ApplyUpcast);
        _downcastBuilder.Properties(_maps.ApplyDowncast);
        _upcast = _upcastBuilder.ToJson();
        _downcast = _downcastBuilder.ToJson();
    }

    [Fact] void should_resolve_the_upcast_paths() => _upcast["WireTarget.NewStatus"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("WireSource.OldStatus");
    [Fact] void should_resolve_the_downcast_paths() => _downcast["WireSource.OldStatus"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("WireTarget.NewStatus");
    [Fact] void should_preserve_the_upcast_values() => _upcast["WireTarget.NewStatus"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("new");
    [Fact] void should_invert_the_downcast_values() => _downcast["WireSource.OldStatus"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("old");
}

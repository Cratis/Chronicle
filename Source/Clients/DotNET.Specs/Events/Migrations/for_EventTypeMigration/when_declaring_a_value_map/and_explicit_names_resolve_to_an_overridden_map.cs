// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_declaring_a_value_map;

public class and_explicit_names_resolve_to_an_overridden_map : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new for_EventMigrationBuilder.given.PrefixPolicy());

    void Because()
    {
        ((IEventTypeMigration)new Migration()).Downcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_emit_only_one_target_mapping() => _result.Count.ShouldEqual(1);
    [Fact] void should_resolve_the_source_name() => _result["OldWireStatus"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("NewWireStatus");
    [Fact] void should_keep_the_explicit_directional_mapping() => _result["OldWireStatus"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("explicit");

    [EventType("resolved-map-override", generation: 2)]
    record Current([property: JsonPropertyName("NewWireStatus")] string Status);

    [EventTypeGenerationFor<Current>(1)]
    record Previous([property: JsonPropertyName("OldWireStatus")] string Status);

    class Migration : EventTypeMigration<Current, Previous>
    {
        public override void Upcast(IEventMigrationBuilder<Current, Previous> builder)
        {
        }

        public override void Downcast(IEventMigrationBuilder<Previous, Current> builder) =>
            builder.Properties(properties => properties.MapValues(target => target.Status, source => source.Status, map => map.Map("new", "explicit")));

        public override void MapValues(IEventValueMapBuilder<Current, Previous> builder) =>
            builder.For(target => target.Status, source => source.Status, map => map.Map("old", "new"));
    }
}

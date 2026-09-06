// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilderFor;

public class when_mapping_values : Specification
{
    EventMigrationPropertyBuilder _inner;
    EventMigrationPropertyBuilderFor<TargetEvent, SourceEvent> _builder;

    void Establish()
    {
        _inner = new EventMigrationPropertyBuilder();
        _builder = new EventMigrationPropertyBuilderFor<TargetEvent, SourceEvent>(_inner);
    }

    void Because() => _builder.MapValues(t => t.Age, s => s.FullName, map => map.Map("unknown", 0));

    [Fact] void should_have_property_keyed_by_target_name() => _inner.Properties.ContainsKey("Age").ShouldBeTrue();

    [Fact] void should_have_map_values_expression() => _inner.Properties["Age"].ToString().ShouldContain("$mapValues");

    [Fact] void should_read_from_the_source_property() =>
        _inner.Properties["Age"]["$mapValues"]["source"].GetValue<string>().ShouldEqual("FullName");

    [Fact] void should_carry_the_declared_mapping() =>
        _inner.Properties["Age"]["$mapValues"]["mappings"][0]["from"].GetValue<string>().ShouldEqual("unknown");
}

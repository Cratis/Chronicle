// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_mapping_values : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _builder.MapValues("Status", "State", [new ValueMapping(0, 10), new ValueMapping(1, 11)]);

    [Fact] void should_have_property_keyed_by_target_name() => _builder.Properties.ContainsKey("Status").ShouldBeTrue();

    [Fact] void should_have_map_values_expression() =>
        _builder.Properties["Status"]["$mapValues"].ShouldNotBeNull();

    [Fact] void should_read_from_the_source_property() =>
        _builder.Properties["Status"]["$mapValues"]["source"].GetValue<string>().ShouldEqual("State");

    [Fact] void should_carry_every_mapping() =>
        _builder.Properties["Status"]["$mapValues"]["mappings"].AsArray().Count.ShouldEqual(2);

    [Fact] void should_carry_the_value_being_mapped_from() =>
        _builder.Properties["Status"]["$mapValues"]["mappings"][0]["from"].GetValue<int>().ShouldEqual(0);

    [Fact] void should_carry_the_value_being_mapped_to() =>
        _builder.Properties["Status"]["$mapValues"]["mappings"][0]["to"].GetValue<int>().ShouldEqual(10);
}

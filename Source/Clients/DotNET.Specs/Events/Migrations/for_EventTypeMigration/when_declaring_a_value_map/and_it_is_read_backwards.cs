// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_declaring_a_value_map;

/// <summary>
/// The point of stating the change as a map is that the downcast falls out of it - the migration never wrote a
/// reverse map, and stating it twice is how the two directions drift apart.
/// </summary>
public class and_it_is_read_backwards : given.a_declared_value_map
{
    [Fact] void should_produce_a_map_values_expression() => _downcast["Status"]["$mapValues"].ShouldNotBeNull();

    [Fact] void should_read_from_the_upgraded_generations_property() =>
        _downcast["Status"]["$mapValues"]["source"].GetValue<string>().ShouldEqual("Status");

    [Fact] void should_carry_every_declared_mapping() =>
        _downcast["Status"]["$mapValues"]["mappings"].AsArray().Count.ShouldEqual(3);

    [Fact] void should_map_from_the_upgraded_member() =>
        Mapping(_downcast, 1)["from"].GetValue<int>().ShouldEqual((int)ValueMapTestStatus.Confirmed);

    [Fact] void should_map_to_the_previous_member() =>
        Mapping(_downcast, 1)["to"].GetValue<int>().ShouldEqual((int)ValueMapTestStatusV1.Verified);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_declaring_a_value_map;

/// <summary>
/// Upcasting reads the map exactly as it was written - from the previous generation's member to the upgraded one's.
/// </summary>
public class and_it_is_read_forward : given.a_declared_value_map
{
    [Fact] void should_produce_a_map_values_expression() => _upcast["Status"]["$mapValues"].ShouldNotBeNull();

    [Fact] void should_read_from_the_previous_generations_property() =>
        _upcast["Status"]["$mapValues"]["source"].GetValue<string>().ShouldEqual("Status");

    [Fact] void should_carry_every_declared_mapping() =>
        _upcast["Status"]["$mapValues"]["mappings"].AsArray().Count.ShouldEqual(3);

    [Fact] void should_map_from_the_previous_member() =>
        Mapping(_upcast, 1)["from"].GetValue<int>().ShouldEqual((int)ValueMapTestStatusV1.Verified);

    [Fact] void should_map_to_the_upgraded_member() =>
        Mapping(_upcast, 1)["to"].GetValue<int>().ShouldEqual((int)ValueMapTestStatus.Confirmed);
}

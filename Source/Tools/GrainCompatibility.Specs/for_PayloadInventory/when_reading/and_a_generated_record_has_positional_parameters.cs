// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_generated_record_has_positional_parameters : given.an_inventory
{
    [Fact] void should_match_the_primary_constructor_ids_written_by_the_generated_codec() => Contract<Positional>().Members.Select(_ => _.Id!.Value).Order().ShouldEqual(given.wire_field_ids.Of(new Positional("value", 2)).Order());

    [Fact] void should_include_the_first_constructor_field() => Contract<Positional>().Members.Any(_ => _.Name == "Name" && _.Id == 0).ShouldBeTrue();
    [Fact] void should_include_the_second_constructor_field() => Contract<Positional>().Members.Any(_ => _.Name == "Count" && _.Id == 1).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_generated_field_ids_are_enabled : given.an_inventory
{
    [Fact] void should_match_the_ids_written_by_the_generated_codec() => Contract<AutoFields>().Members.Select(_ => _.Id!.Value).Order().ShouldEqual(given.wire_field_ids.Of(new AutoFields { Name = "value", Count = 42, Status = Status.Active, Optional = 7, Labels = ["one"], Values = [1] }).Order());

    [Fact] void should_include_automatically_numbered_properties_but_not_the_computed_getter() => Contract<AutoFields>().Members.Count.ShouldEqual(6);
}

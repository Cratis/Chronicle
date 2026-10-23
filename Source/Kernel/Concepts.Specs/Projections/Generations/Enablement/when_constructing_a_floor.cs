// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

public class when_constructing_a_floor : Specification
{
    GenerationFloor _floor;
    void Because() => _floor = new(new(1), new(1), [new("unknown.z"), GenerationCapability.FloorAdmission, new("unknown.z")], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", "");
    [Fact] void should_sort_and_deduplicate_capabilities() => _floor.RequiredCapabilities.Select(_ => _.Value).ShouldEqual(["FloorAdmission", "unknown.z"]);
    [Fact] void should_not_advertise_unimplemented_adoption() => GenerationBinarySupport.Current.Capabilities.Contains(GenerationCapability.InPlaceAdoption).ShouldBeFalse();
    [Fact] void should_not_advertise_unimplemented_binding() => GenerationBinarySupport.Current.Capabilities.Contains(GenerationCapability.BindingEpoch).ShouldBeFalse();
}

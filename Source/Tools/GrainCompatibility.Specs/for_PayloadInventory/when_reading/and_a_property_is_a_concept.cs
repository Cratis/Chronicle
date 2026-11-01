// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_property_is_a_concept : given.an_inventory
{
    PayloadTypeContract _contract;

    void Because() => _contract = Contract<Id>();

    [Fact] void should_use_the_concept_representation() => _contract.Kind.ShouldEqual(PayloadKind.Concept);
    [Fact] void should_record_the_underlying_type() => _contract.Underlying.ShouldEqual("string");
    [Fact] void should_not_expose_concept_internals() => _contract.Members.ShouldBeEmpty();
}

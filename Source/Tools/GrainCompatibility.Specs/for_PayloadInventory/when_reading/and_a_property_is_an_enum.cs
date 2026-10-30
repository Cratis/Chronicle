// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_property_is_an_enum : given.an_inventory
{
    PayloadTypeContract _contract;

    void Because() => _contract = Contract<Status>();

    [Fact] void should_use_the_enum_representation() => _contract.Kind.ShouldEqual(PayloadKind.Enum);
    [Fact] void should_record_the_underlying_type() => _contract.Underlying.ShouldEqual("int");
    [Fact] void should_record_numeric_values() => _contract.Members.Single(_ => _.Name == "Paused").Type.ShouldEqual("2");
}

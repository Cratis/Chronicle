// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_type_has_a_custom_converter : given.an_inventory
{
    [Fact] void should_record_the_custom_representation() => Contract<Custom>().Kind.ShouldEqual(PayloadKind.Custom);
    [Fact] void should_not_record_hidden_members() => Contract<Custom>().Members.ShouldBeEmpty();
}

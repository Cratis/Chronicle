// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_property_is_declared_as_an_abstract_payload : given.an_inventory
{
    [Fact] void should_record_the_declared_type() => Contract<AbstractPayload>().Kind.ShouldEqual(PayloadKind.Json);
    [Fact] void should_record_concrete_subtypes() => Contract<ConcretePayload>().Members.Single().Name.ShouldEqual("value");
}

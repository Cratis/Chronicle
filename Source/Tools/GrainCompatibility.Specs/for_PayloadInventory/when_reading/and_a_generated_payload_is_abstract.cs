// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_generated_payload_is_abstract : given.an_inventory
{
    [Fact] void should_discover_generated_concrete_subtypes() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(GeneratedSubtype))).ShouldBeTrue();
}

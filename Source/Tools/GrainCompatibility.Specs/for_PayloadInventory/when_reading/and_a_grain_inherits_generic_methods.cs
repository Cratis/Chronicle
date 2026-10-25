// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_grain_inherits_generic_methods : given.an_inventory
{
    [Fact] void should_walk_the_constructed_base_method_parameter() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(InheritedRequest))).ShouldBeTrue();
}

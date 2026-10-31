// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_property_is_a_grain_reference_or_an_exception : given.an_inventory
{
    [Fact] void should_not_record_grain_references() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(IFixtureGrain))).ShouldBeFalse();
    [Fact] void should_not_record_exceptions() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(Exception))).ShouldBeFalse();
}

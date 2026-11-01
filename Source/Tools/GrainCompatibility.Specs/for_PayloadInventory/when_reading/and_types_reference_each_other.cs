// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_types_reference_each_other : given.an_inventory
{
    [Fact] void should_record_the_cycle_once() => _manifest.Types.Values.Count(_ => _.ClrName == PayloadTypeName.Of(typeof(Cycle))).ShouldEqual(1);
    [Fact] void should_record_the_parent_once() => _manifest.Types.Values.Count(_ => _.ClrName == PayloadTypeName.Of(typeof(Payload))).ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_a_property_is_a_collection_of_payloads : given.an_inventory
{
    [Fact] void should_walk_collection_elements() => Contract<Child>().Kind.ShouldEqual(PayloadKind.Json);
    [Fact] void should_not_record_the_collection_itself() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(IEnumerable<Child>))).ShouldBeFalse();
    [Fact] void should_not_record_the_dictionary_itself() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(Dictionary<string, Child>))).ShouldBeFalse();
    [Fact] void should_not_record_the_array_itself() => _manifest.Types.ContainsKey(PayloadTypeName.Of(typeof(Child[]))).ShouldBeFalse();
}

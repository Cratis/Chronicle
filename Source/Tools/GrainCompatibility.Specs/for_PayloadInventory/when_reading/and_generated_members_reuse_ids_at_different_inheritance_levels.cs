// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;
using static Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given.payload_types;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading;

public class and_generated_members_reuse_ids_at_different_inheritance_levels : given.an_inventory
{
    [Fact] void should_preserve_base_and_derived_members() => Contract<GeneratedDerived>().Members.Count.ShouldEqual(2);
    [Fact] void should_compare_each_inheritance_level_independently()
    {
        var original = _manifest.Types.ToDictionary(_ => _.Key, _ => _.Value);
        var type = Contract<GeneratedDerived>();
        original[type.Identity] = type with { Members = type.Members.Select(_ => _.Level == 1 ? _ with { Type = "int" } : _).ToArray() };
        PayloadContractComparison.Compare(_manifest, new(original)).Select(_ => _.Kind).ShouldEqual([ContractBreakKind.PayloadPropertyTypeChanged]);
    }

    [Fact] void should_round_trip_the_inventory() => PayloadContractManifest.Parse(_manifest.ToText()).ToText().ShouldEqual(_manifest.ToText());
}

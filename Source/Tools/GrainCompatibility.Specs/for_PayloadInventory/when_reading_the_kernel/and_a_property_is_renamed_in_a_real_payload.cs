// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading_the_kernel;

public class and_a_property_is_renamed_in_a_real_payload : given.the_kernel_inventory
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because()
    {
        var original = FirstWithMembers(PayloadKind.Json);
        _breaks = CompareChanged(original, original with { Members = [original.Members[0] with { Name = "renamedForPlantedBreak" }, .. original.Members.Skip(1)] });
    }

    [Fact] void should_detect_only_the_renamed_property() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.PayloadPropertyRemoved]);
}

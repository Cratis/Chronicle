// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading_the_kernel;

public class and_an_enum_member_is_removed_from_a_real_payload : given.the_kernel_inventory
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because()
    {
        var original = FirstWithMembers(PayloadKind.Enum, 2);
        _breaks = CompareChanged(original, original with { Members = original.Members.Skip(1).ToArray() });
    }

    [Fact] void should_detect_only_the_removed_enum_member() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.EnumMemberRemoved]);
}

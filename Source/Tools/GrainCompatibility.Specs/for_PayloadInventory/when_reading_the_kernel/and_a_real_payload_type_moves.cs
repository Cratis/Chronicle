// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading_the_kernel;

public class and_a_real_payload_type_moves : given.the_kernel_inventory
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because()
    {
        var original = FirstWithMembers(PayloadKind.Json);
        var name = "Cratis.PlantedMove." + original.ClrName;
        _breaks = CompareChanged(original, original with { Identity = name, ClrName = name });
    }

    [Fact] void should_detect_only_the_moved_type() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.PayloadTypeRemoved]);
}

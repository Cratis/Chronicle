// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading_the_kernel;

public class and_a_property_is_added_to_a_real_payload : given.the_kernel_inventory
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because()
    {
        var original = FirstWithMembers(PayloadKind.Json);
        _breaks = CompareChanged(original, original with { Members = [.. original.Members, new("addedForPlantedChange", "string")] });
    }

    [Fact] void should_keep_serving_the_previous_release() => _breaks.ShouldBeEmpty();
}

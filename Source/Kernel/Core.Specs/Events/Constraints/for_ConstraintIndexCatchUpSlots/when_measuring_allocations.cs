// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexCatchUpSlots;

public class when_measuring_allocations : Specification
{
    long _bytes;

    async Task Because()
    {
        var storage = Substitute.For<IStorage>();
        ConstraintIndexCatchUpSlots.For(storage, ConstraintIndexRepairOptions.Default.CatchUpSlots);
        (_bytes, _) = await AllocationProbe.Measure("slots-for-existing", 100000, () =>
        {
            ConstraintIndexCatchUpSlots.For(storage, ConstraintIndexRepairOptions.Default.CatchUpSlots);
            return Task.CompletedTask;
        });
    }

    [Fact] void should_stay_below_a_generous_upper_bound() => _bytes.ShouldBeLessThan(1024L);
}

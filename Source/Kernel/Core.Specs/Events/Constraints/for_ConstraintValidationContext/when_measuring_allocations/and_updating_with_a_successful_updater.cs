// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationContext.when_measuring_allocations;

public class and_updating_with_a_successful_updater : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    long _bytes;

    async Task Because() => (_bytes, _) = await AllocationProbe.Measure("context-update", 1000, () => _context.Update(1, TimeSpan.FromSeconds(1), CancellationToken.None));

    [Fact] void should_stay_below_a_generous_upper_bound() => _bytes.ShouldBeLessThan(64L * 1024);
}

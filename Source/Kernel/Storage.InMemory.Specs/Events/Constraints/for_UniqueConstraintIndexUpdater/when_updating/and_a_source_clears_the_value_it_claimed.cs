// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

/// <summary>
/// Clearing a constrained value must release the claim the event source held, or the value stays taken forever.
/// </summary>
public class and_a_source_clears_the_value_it_claimed : given.a_unique_constraint_over_in_memory_storage
{
    ConstraintValidationResult _firstClaim;
    ConstraintValidationResult _clear;
    ConstraintValidationResult _claimByOther;

    async Task Because()
    {
        _firstClaim = await Append("source-a", "foo", 1);
        _clear = await Append("source-a", null, 2);
        _claimByOther = await Append("source-b", "foo", 3);
    }

    [Fact] void should_accept_the_first_claim() => _firstClaim.IsValid.ShouldBeTrue();
    [Fact] void should_accept_clearing_the_value() => _clear.IsValid.ShouldBeTrue();
    [Fact] void should_accept_the_claim_of_the_released_value_by_another_source() => _claimByOther.IsValid.ShouldBeTrue();
}

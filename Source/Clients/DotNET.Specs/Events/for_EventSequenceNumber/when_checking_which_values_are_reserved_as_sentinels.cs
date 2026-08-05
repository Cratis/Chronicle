// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventSequenceNumber;

/// <summary>
/// The sentinels are wire values. The kernel has its own copy of <see cref="EventSequenceNumber"/> and the two are
/// compared across the wire - <c>ConcurrencyScope.NotSet</c> is built from <c>Max</c> on both sides - so the numbers
/// pinned here have to be the same numbers the kernel's copy pins.
/// </summary>
public class when_checking_which_values_are_reserved_as_sentinels : Specification
{
    [Fact] void should_start_counting_at_zero() => EventSequenceNumber.First.Value.ShouldEqual(0UL);
    [Fact] void should_reserve_the_topmost_value_for_unavailable() => EventSequenceNumber.Unavailable.Value.ShouldEqual(ulong.MaxValue);
    [Fact] void should_reserve_the_value_below_it_for_max() => EventSequenceNumber.Max.Value.ShouldEqual(ulong.MaxValue - 1);
    [Fact] void should_not_treat_unavailable_as_an_actual_value() => EventSequenceNumber.Unavailable.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_treat_max_as_an_actual_value() => EventSequenceNumber.Max.IsActualValue.ShouldBeFalse();
    [Fact] void should_treat_the_first_number_as_an_actual_value() => EventSequenceNumber.First.IsActualValue.ShouldBeTrue();
    [Fact] void should_reserve_nothing_below_max() => new EventSequenceNumber(ulong.MaxValue - 2).IsActualValue.ShouldBeTrue();
}

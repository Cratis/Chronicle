// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventSequenceNumber;

/// <summary>
/// The sentinels are wire values. The client has its own copy of <see cref="EventSequenceNumber"/> and the two are
/// compared across the wire - <c>ConcurrencyScope.NotSet</c> is built from <c>Max</c> on both sides, and a concurrency
/// scope the client resolved to <c>BeforeFirst</c> is validated here as "no event matching this scope may exist" - so
/// the numbers pinned here have to be the same numbers the client's copy pins.
/// </summary>
public class when_checking_which_values_are_reserved_as_sentinels : Specification
{
    [Fact] void should_start_counting_at_zero() => EventSequenceNumber.First.Value.ShouldEqual(0UL);
    [Fact] void should_reserve_the_topmost_value_for_unavailable() => EventSequenceNumber.Unavailable.Value.ShouldEqual(ulong.MaxValue);
    [Fact] void should_reserve_the_value_below_it_for_max() => EventSequenceNumber.Max.Value.ShouldEqual(ulong.MaxValue - 1);
    [Fact] void should_reserve_the_value_below_max_for_before_first() => EventSequenceNumber.BeforeFirst.Value.ShouldEqual(ulong.MaxValue - 2);
    [Fact] void should_not_treat_unavailable_as_an_actual_value() => EventSequenceNumber.Unavailable.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_treat_max_as_an_actual_value() => EventSequenceNumber.Max.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_treat_before_first_as_an_actual_value() => EventSequenceNumber.BeforeFirst.IsActualValue.ShouldBeFalse();
    [Fact] void should_treat_the_first_number_as_an_actual_value() => EventSequenceNumber.First.IsActualValue.ShouldBeTrue();
    [Fact] void should_reserve_nothing_below_before_first() => new EventSequenceNumber(ulong.MaxValue - 3).IsActualValue.ShouldBeTrue();
    [Fact] void should_not_confuse_before_first_with_unavailable() => EventSequenceNumber.BeforeFirst.IsUnavailable.ShouldBeFalse();
    [Fact] void should_not_confuse_unavailable_with_before_first() => EventSequenceNumber.Unavailable.IsBeforeFirst.ShouldBeFalse();
}

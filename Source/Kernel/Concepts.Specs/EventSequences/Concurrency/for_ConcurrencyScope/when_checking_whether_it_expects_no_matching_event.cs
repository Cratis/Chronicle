// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScope;

/// <summary>
/// The expectation a first append actually has is "no event matching this narrowing exists yet", and
/// <see cref="EventSequenceNumber.BeforeFirst"/> is the only value that can say it. The two scopes that opt out of
/// checking must not be read as saying it - <see cref="ConcurrencyScope.None"/> in particular carries
/// <see cref="EventSequenceNumber.Unavailable"/>, and a scope that expects nothing to exist is a check, not an opt-out.
/// </summary>
public class when_checking_whether_it_expects_no_matching_event : Specification
{
    [Fact] void should_report_a_scope_expecting_the_position_before_the_first_event() =>
        new ConcurrencyScope(EventSequenceNumber.BeforeFirst, true, null, null, new EventSourceType("Thing"), null).ExpectsNoMatchingEvent.ShouldBeTrue();

    [Fact] void should_not_report_a_scope_expecting_an_actual_sequence_number() =>
        new ConcurrencyScope(42UL, true, null, null, new EventSourceType("Thing"), null).ExpectsNoMatchingEvent.ShouldBeFalse();

    [Fact] void should_not_report_a_scope_that_says_nothing_about_what_it_expects() =>
        new ConcurrencyScope(EventSequenceNumber.Unavailable, true, null, null, new EventSourceType("Thing"), null).ExpectsNoMatchingEvent.ShouldBeFalse();

    [Fact] void should_not_report_the_not_set_scope() => ConcurrencyScope.NotSet.ExpectsNoMatchingEvent.ShouldBeFalse();
    [Fact] void should_not_report_the_none_scope() => ConcurrencyScope.None.ExpectsNoMatchingEvent.ShouldBeFalse();
}

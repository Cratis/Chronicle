// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeBuilder;

/// <summary>
/// The per-append opt-in. A caller that wants "only one writer may open this partition" for one behavior does not
/// have to turn the check on for the whole application - it declares the expectation on the scope it builds, and
/// the kernel checks that append.
/// </summary>
public class when_expecting_no_matching_event : Specification
{
    static readonly EventSourceType _eventSourceType = new("Customer");

    ConcurrencyScope _result;

    void Because() => _result = new ConcurrencyScopeBuilder()
        .ExpectingNoMatchingEvent()
        .WithEventSourceType(_eventSourceType)
        .Build();

    [Fact] void should_expect_no_event_matching_the_narrowing() => _result.ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_expect_the_position_before_the_first_event() => _result.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_not_produce_a_scope_the_kernel_skips() => _result.IsIncomplete.ShouldBeFalse();
    [Fact] void should_keep_the_narrowing() => _result.EventSourceType.ShouldEqual(_eventSourceType);
}

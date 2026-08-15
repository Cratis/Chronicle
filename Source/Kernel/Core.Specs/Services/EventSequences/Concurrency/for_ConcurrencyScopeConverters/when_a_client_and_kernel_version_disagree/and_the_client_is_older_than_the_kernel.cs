// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_a_client_and_kernel_version_disagree;

/// <summary>
/// An older client resolving an empty narrowing sends the "unavailable" sequence number and nothing else - it has
/// never heard of the dedicated field, so the field arrives unset. The kernel must read that as the older client
/// meant it: no expectation it can check. It skips, and reports the append as unchecked.
/// </summary>
public class and_the_client_is_older_than_the_kernel : given.a_validator_reading_what_arrived_on_the_wire
{
    void Establish() => MatchingEventExistsAt(7UL);

    async Task Because() => await Validate(FirstAppendIntoANarrowedScope(declaresTheExpectation: false));

    [Fact] void should_not_read_it_as_expecting_no_matching_event() => _scope.ExpectsNoMatchingEvent.ShouldBeFalse();
    [Fact] void should_treat_it_as_the_incomplete_scope_it_has_always_been() => _scope.IsIncomplete.ShouldBeTrue();
    [Fact] void should_report_the_append_as_unchecked() => _scope.ShouldBeValidated.ShouldBeFalse();
    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_not_read_a_tail_to_compare_against() =>
        _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
}

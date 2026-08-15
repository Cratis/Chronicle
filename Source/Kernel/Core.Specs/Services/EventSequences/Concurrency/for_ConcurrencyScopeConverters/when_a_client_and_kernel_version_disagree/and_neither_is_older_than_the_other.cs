// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_a_client_and_kernel_version_disagree;

/// <summary>
/// The control for the two mismatch specs beside this one. They assert that a skewed pair does not check, against
/// a substitute that would report a violation if it did - and "did not check" is what a broken harness produces
/// too. This runs the same wire scope and the same validator with both ends current, on the same seeded tail, and
/// gets the violation. So the mismatch specs are showing the fallback, not an inert setup.
/// </summary>
public class and_neither_is_older_than_the_other : given.a_validator_reading_what_arrived_on_the_wire
{
    void Establish() => MatchingEventExistsAt(7UL);

    async Task Because() => await Validate(FirstAppendIntoANarrowedScope(declaresTheExpectation: true));

    [Fact] void should_read_it_as_expecting_no_matching_event() => _scope.ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_check_it() => _scope.ShouldBeValidated.ShouldBeTrue();
    [Fact] void should_reject_the_append_because_a_matching_event_exists() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_report_the_sequence_number_of_what_is_there_instead() => _result.AsT0.ActualSequenceNumber.Value.ShouldEqual(7UL);
}

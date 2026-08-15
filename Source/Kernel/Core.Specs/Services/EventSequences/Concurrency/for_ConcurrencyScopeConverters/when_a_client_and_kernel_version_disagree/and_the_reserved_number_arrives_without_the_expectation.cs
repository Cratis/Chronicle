// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_a_client_and_kernel_version_disagree;

/// <summary>
/// The sequence-number field carries no intent of its own, and this is what says so. No client puts the reserved
/// before-first value in it - the .NET client rewrites it, and the other language clients build the contract
/// directly - but a scope that arrives with it and no expectation set did not come from something that meant the
/// expectation. Promoting it would let a check appear out of a number rather than out of what the caller asked
/// for, which is the same class of accident in the opposite direction. It is downgraded to the value the validator
/// declines, and skipped.
/// </summary>
public class and_the_reserved_number_arrives_without_the_expectation : given.a_validator_reading_what_arrived_on_the_wire
{
    void Establish() => MatchingEventExistsAt(7UL);

    async Task Because() => await Validate(new Contracts.EventSequences.Concurrency.ConcurrencyScope
    {
        SequenceNumber = EventSequenceNumber.BeforeFirst,
        ExpectsNoMatchingEvent = false,
        EventSourceId = true,
        EventSourceType = "Customer"
    });

    [Fact] void should_not_read_it_as_expecting_no_matching_event() => _scope.ExpectsNoMatchingEvent.ShouldBeFalse();
    [Fact] void should_downgrade_it_to_the_number_the_validator_declines() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_skip_the_check() => _scope.ShouldBeValidated.ShouldBeFalse();
    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
}

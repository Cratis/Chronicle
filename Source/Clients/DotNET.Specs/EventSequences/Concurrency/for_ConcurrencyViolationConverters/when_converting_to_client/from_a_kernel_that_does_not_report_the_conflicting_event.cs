// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyViolationConverters.when_converting_to_client;

public class from_a_kernel_that_does_not_report_the_conflicting_event : Specification
{
    Contracts.EventSequences.Concurrency.ConcurrencyViolation _contractViolation;
    ConcurrencyViolation _result;

    void Establish() => _contractViolation = new()
    {
        EventSourceId = "source",
        ExpectedSequenceNumber = 42ul,
        ActualSequenceNumber = 45ul
    };

    void Because() => _result = _contractViolation.ToClient();

    [Fact] void should_not_set_a_conflicting_event_type() => _result.ConflictingEventTypeId.ShouldBeNull();
    [Fact] void should_not_set_a_conflicting_event_sequence_number() => _result.ConflictingEventSequenceNumber.ShouldBeNull();
}

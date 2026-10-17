// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyViolationConverters.when_converting_to_client;

public class with_a_conflicting_event : Specification
{
    Contracts.EventSequences.Concurrency.ConcurrencyViolation _contractViolation;
    ConcurrencyViolation _result;

    void Establish() => _contractViolation = new()
    {
        EventSourceId = "source",
        ExpectedSequenceNumber = 42ul,
        ActualSequenceNumber = 45ul,
        ConflictingEventTypeId = "BookBorrowed",
        ConflictingSequenceNumber = 43ul
    };

    void Because() => _result = _contractViolation.ToClient();

    [Fact] void should_set_the_conflicting_event_type() => _result.ConflictingEventTypeId.ShouldEqual(new EventTypeId("BookBorrowed"));
    [Fact] void should_set_the_conflicting_event_sequence_number() => _result.ConflictingEventSequenceNumber.ShouldEqual(new EventSequenceNumber(43ul));
}

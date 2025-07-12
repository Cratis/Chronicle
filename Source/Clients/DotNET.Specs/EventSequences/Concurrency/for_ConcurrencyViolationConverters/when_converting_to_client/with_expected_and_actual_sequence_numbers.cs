// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyViolationConverters.when_converting_to_client;

public class with_expected_and_actual_sequence_numbers : Specification
{
    Contracts.EventSequences.Concurrency.ConcurrencyViolation _contractViolation;
    ConcurrencyViolation _result;

    void Establish()
    {
        _contractViolation = new Contracts.EventSequences.Concurrency.ConcurrencyViolation
        {
            ExpectedSequenceNumber = 42ul,
            ActualSequenceNumber = 43ul
        };
    }

    void Because() => _result = _contractViolation.ToClient();

    [Fact] void should_set_expected_event_sequence_number() => _result.ExpectedEventSequenceNumber.ShouldEqual(new EventSequenceNumber(42ul));
    [Fact] void should_set_actual_event_sequence_number() => _result.ActualEventSequenceNumber.ShouldEqual(new EventSequenceNumber(43ul));
}

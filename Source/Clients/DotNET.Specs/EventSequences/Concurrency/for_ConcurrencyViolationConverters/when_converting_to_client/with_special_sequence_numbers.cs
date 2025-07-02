// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyViolationConverters.when_converting_to_client;

public class with_special_sequence_numbers : Specification
{
    Contracts.EventSequences.Concurrency.ConcurrencyViolation _contractViolation;
    ConcurrencyViolation _result;

    void Establish()
    {
        _contractViolation = new Contracts.EventSequences.Concurrency.ConcurrencyViolation
        {
            ExpectedSequenceNumber = 0ul,
            ActualSequenceNumber = ulong.MaxValue
        };
    }

    void Because() => _result = _contractViolation.ToClient();

    [Fact] void should_handle_zero_expected_sequence_number() => _result.ExpectedEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_handle_max_actual_sequence_number() => _result.ActualEventSequenceNumber.ShouldEqual(new EventSequenceNumber(ulong.MaxValue));
}

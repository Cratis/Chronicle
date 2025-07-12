// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult;

public class when_creating_success_result : Specification
{
    CorrelationId _correlationId;
    IEnumerable<EventSequenceNumber> _sequenceNumbers;
    AppendManyResult _result;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _sequenceNumbers = [(EventSequenceNumber)1UL, (EventSequenceNumber)2UL, (EventSequenceNumber)3UL];
    }

    void Because() => _result = AppendManyResult.Success(_correlationId, _sequenceNumbers);

    [Fact] void should_set_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_set_sequence_numbers() => _result.SequenceNumbers.ShouldEqual(_sequenceNumbers);
    [Fact] void should_be_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_constraint_violations() => _result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_have_no_concurrency_violations() => _result.ConcurrencyViolations.ShouldBeEmpty();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
}

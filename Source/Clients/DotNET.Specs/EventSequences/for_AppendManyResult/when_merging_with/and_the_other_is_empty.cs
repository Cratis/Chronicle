// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult.when_merging_with;

public class and_the_other_is_empty : Specification
{
    AppendManyResult _first;
    AppendManyResult _second;
    AppendManyResult _result;
    CorrelationId _correlationId1;
    CorrelationId _correlationId2;
    EventSequenceNumber _seq1;
    ConstraintViolation _constraintViolation1;
    ConcurrencyViolation _concurrencyViolation1;
    AppendError _error1;

    void Establish()
    {
        _correlationId1 = CorrelationId.New();
        _correlationId2 = CorrelationId.New();
        _seq1 = new(1);
        _constraintViolation1 = new(
            EventTypeId.Unknown,
            _seq1,
            "constraint1",
            "reason1",
            new ConstraintViolationDetails());
        _concurrencyViolation1 = new(EventSequenceId.Log, new { Some = "Event" });
        _error1 = new("error1");

        _first = new AppendManyResult
        {
            CorrelationId = _correlationId1,
            SequenceNumbers = [_seq1],
            ConstraintViolations = [_constraintViolation1],
            ConcurrencyViolations = [_concurrencyViolation1],
            Errors = [_error1]
        };
        _second = new AppendManyResult
        {
            CorrelationId = _correlationId2
        };
    }

    void Because() => _result = _first.MergeWith(_second);

    [Fact] void should_use_the_others_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId2);
    [Fact] void should_merge_sequence_numbers() => _result.SequenceNumbers.ShouldContain(_seq1);
    [Fact] void should_merge_constraint_violations() => _result.ConstraintViolations.ShouldContain(_constraintViolation1);
    [Fact] void should_merge_concurrency_violations() => _result.ConcurrencyViolations.ShouldContain(_concurrencyViolation1);
    [Fact] void should_merge_errors() => _result.Errors.ShouldContain(_error1);
}

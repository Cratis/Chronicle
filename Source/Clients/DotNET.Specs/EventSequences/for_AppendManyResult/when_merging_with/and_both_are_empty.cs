// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult.when_merging_with;

public class and_both_are_empty : Specification
{
    AppendManyResult _first;
    AppendManyResult _second;
    AppendManyResult _result;
    CorrelationId _correlationId1;
    CorrelationId _correlationId2;

    void Establish()
    {
        _correlationId1 = CorrelationId.New();
        _correlationId2 = CorrelationId.New();
        _first = new AppendManyResult { CorrelationId = _correlationId1 };
        _second = new AppendManyResult { CorrelationId = _correlationId2 };
    }

    void Because() => _result = _first.MergeWith(_second);

    [Fact] void should_use_the_others_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId2);
    [Fact] void should_have_no_sequence_numbers() => _result.SequenceNumbers.ShouldBeEmpty();
    [Fact] void should_have_no_constraint_violations() => _result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_have_no_concurrency_violations() => _result.ConcurrencyViolations.ShouldBeEmpty();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
}

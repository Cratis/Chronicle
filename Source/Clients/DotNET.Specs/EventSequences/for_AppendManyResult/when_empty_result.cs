// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult;

public class when_empty_result : Specification
{
    AppendManyResult _result;

    void Establish() => _result = new AppendManyResult();

    [Fact] void should_have_default_correlation_id() => _result.CorrelationId.ShouldEqual(CorrelationId.NotSet);
    [Fact] void should_have_empty_sequence_numbers() => _result.SequenceNumbers.ShouldBeEmpty();
    [Fact] void should_not_have_constraint_violations() => _result.HasConstraintViolations.ShouldBeFalse();
    [Fact] void should_not_have_concurrency_violations() => _result.HasConcurrencyViolations.ShouldBeFalse();
    [Fact] void should_not_have_errors() => _result.HasErrors.ShouldBeFalse();
    [Fact] void should_be_success() => _result.IsSuccess.ShouldBeTrue();
}

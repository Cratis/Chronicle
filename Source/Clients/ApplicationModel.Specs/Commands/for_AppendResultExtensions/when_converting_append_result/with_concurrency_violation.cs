// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Applications.Commands.for_AppendResultExtensions.when_converting_append_result;

public class with_concurrency_violation : given.all_dependencies
{
    AppendResult _appendResult;
    CommandResult _result;
    ConcurrencyViolation _violation;

    void Establish()
    {
        _violation = new ConcurrencyViolation(
            EventSourceId.New(),
            new EventSequenceNumber(10),
            new EventSequenceNumber(15));

        _appendResult = new AppendResult
        {
            CorrelationId = _correlationId,
            ConcurrencyViolation = _violation
        };
    }

    void Because() => _result = _appendResult.ToCommandResult();

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_concurrency_violation_message() => _result.ValidationResults.First().Message.ShouldContain("Concurrency violation");
    [Fact] void should_include_expected_sequence_number() => _result.ValidationResults.First().Message.ShouldContain("10");
    [Fact] void should_include_actual_sequence_number() => _result.ValidationResults.First().Message.ShouldContain("15");
}

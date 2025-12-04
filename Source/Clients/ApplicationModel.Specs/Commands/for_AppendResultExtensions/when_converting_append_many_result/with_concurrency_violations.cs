// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Applications.Commands.for_AppendResultExtensions.when_converting_append_many_result;

public class with_concurrency_violations : given.all_dependencies
{
    AppendManyResult _appendResult;
    CommandResult _result;

    void Establish()
    {
        var violations = new[]
        {
            new ConcurrencyViolation(
                EventSourceId.New(),
                new EventSequenceNumber(10),
                new EventSequenceNumber(15)),
            new ConcurrencyViolation(
                EventSourceId.New(),
                new EventSequenceNumber(20),
                new EventSequenceNumber(25))
        };

        _appendResult = new AppendManyResult
        {
            CorrelationId = _correlationId,
            ConcurrencyViolations = violations
        };
    }

    void Because() => _result = _appendResult.ToCommandResult();

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_two_validation_results() => _result.ValidationResults.Count().ShouldEqual(2);
    [Fact] void should_include_concurrency_violation_messages() => _result.ValidationResults.All(r => r.Message.Contains("Concurrency violation")).ShouldBeTrue();
}

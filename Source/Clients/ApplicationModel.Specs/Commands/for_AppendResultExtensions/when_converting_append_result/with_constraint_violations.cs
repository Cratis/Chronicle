// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands.for_AppendResultExtensions.when_converting_append_result;

public class with_constraint_violations : given.all_dependencies
{
    AppendResult _appendResult;
    CommandResult _result;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            new ConstraintName("TestConstraint"),
            new ConstraintViolationMessage("Test violation message"),
            new ConstraintViolationDetails());

        _appendResult = AppendResult.Failed(_correlationId, [_violation]);
    }

    void Because() => _result = _appendResult.ToCommandResult();

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_constraint_violation_message() => _result.ValidationResults.First().Message.ShouldEqual("Test violation message");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_handling;

public class and_append_operation_fails : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    AppendResult _appendResult;
    CommandResult _result;

    void Establish()
    {
        _event = new TestEvent("Test Event");
        var violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            new ConstraintName("TestConstraint"),
            new ConstraintViolationMessage("Test violation"),
            new ConstraintViolationDetails());

        _appendResult = AppendResult.Failed(_correlationId, [violation]);
        _eventLog.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_appendResult);
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _event);

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_include_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_include_constraint_violation_message() => _result.ValidationResults.First().Message.ShouldEqual("Test violation");
}

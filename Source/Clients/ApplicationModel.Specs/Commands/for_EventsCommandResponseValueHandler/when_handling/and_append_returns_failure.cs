// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class and_append_returns_failure : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    AppendManyResult _appendResult;
    CommandResult _result;

    void Establish()
    {
        _events = [new TestEvent("Test")];
        var violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            new ConstraintName("TestConstraint"),
            new ConstraintViolationMessage("Test violation"),
            new ConstraintViolationDetails());

        _appendResult = new AppendManyResult
        {
            CorrelationId = _correlationId,
            ConstraintViolations = [violation]
        };
        _eventLog.AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>()).Returns(_appendResult);
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_include_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_include_constraint_violation_message() => _result.ValidationResults.First().Message.ShouldEqual("Test violation");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_handling;

public class append_operation_fails : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    Exception _exception;

    void Establish()
    {
        _event = new TestEvent("Test Event");
        _exception = new InvalidOperationException("Append failed");
        _eventLog.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(Task.FromException<AppendResult>(_exception));
    }

    async Task Because() => _exception = await Catch.Exception(() => _handler.Handle(_commandContext, _event));

    [Fact] void should_propagate_exception() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}

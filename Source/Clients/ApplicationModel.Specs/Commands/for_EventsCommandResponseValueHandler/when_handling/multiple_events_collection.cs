// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class multiple_events_collection : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    CommandResult _result;
    TestEvent _testEvent;
    AnotherTestEvent _anotherTestEvent;

    void Establish()
    {
        _testEvent = new TestEvent("First Event");
        _anotherTestEvent = new AnotherTestEvent(123);
        _events = [_testEvent, _anotherTestEvent];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_events_to_event_log() => _eventLog.Received(1).AppendMany(Arg.Any<EventSourceId>(), _events, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_with_correct_event_source_id() => _eventLog.Received().AppendMany(((TestCommand)_commandContext.Command).EventSourceId, _events, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}

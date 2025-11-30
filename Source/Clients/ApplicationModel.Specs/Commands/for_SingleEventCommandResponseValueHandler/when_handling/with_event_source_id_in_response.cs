// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_handling;

public class with_event_source_id_in_response : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _event = new TestEvent("Single Event");
        _eventSourceId = EventSourceId.New();

        var command = new TestCommand();
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], new(), _eventSourceId);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _event);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_event_to_event_log() => _eventLog.Received(1).Append(_eventSourceId, _event, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_with_event_source_id_from_response() => _eventLog.Received().Append(_eventSourceId, _event, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}

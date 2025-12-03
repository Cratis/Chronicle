// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_handling_with_metadata;

public class with_event_source_id_in_response : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    CommandResult _result;
    EventSourceId _eventSourceId;
    EventStreamType _eventStreamType;

    void Establish()
    {
        _event = new TestEvent("Event with metadata");
        _eventSourceId = EventSourceId.New();
        _eventStreamType = "Onboarding";

        var command = new TestCommand();
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventStreamType, _eventStreamType }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, _eventSourceId);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _event);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_with_event_source_id_from_response() => _eventLog.Received().Append(_eventSourceId, _event, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_with_event_stream_type() => _eventLog.Received().Append(Arg.Any<EventSourceId>(), _event, _eventStreamType, Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_handling_with_metadata;

public class with_event_source_id_in_response : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    CommandResult _result;
    EventSourceId _eventSourceId;
    EventSourceType _eventSourceType;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;

    void Establish()
    {
        _events = [new TestEvent("Event with metadata")];
        _eventSourceId = EventSourceId.New();
        _eventSourceType = "Account";
        _eventStreamType = "Onboarding";
        _eventStreamId = "Monthly";

        var command = new TestCommand();
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceType, _eventSourceType },
            { WellKnownCommandContextKeys.EventStreamType, _eventStreamType },
            { WellKnownCommandContextKeys.EventStreamId, _eventStreamId }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, _eventSourceId);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_with_event_source_id_from_response() => _eventLog.Received().AppendMany(_eventSourceId, Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_with_event_source_type() => _eventLog.Received().AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), _eventStreamType, _eventStreamId, _eventSourceType);
    [Fact] void should_append_with_event_stream_type() => _eventLog.Received().AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), _eventStreamType, Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_with_event_stream_id() => _eventLog.Received().AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), _eventStreamId, Arg.Any<EventSourceType?>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_handling_with_metadata;

public class with_concurrency_on_event_stream_id : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    CommandResult _result;
    EventStreamId _eventStreamId;

    void Establish()
    {
        _events = [new TestEvent("Event with concurrency")];
        _eventStreamId = "Monthly";

        var command = new TestCommand { EventSourceId = EventSourceId.New() };
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, command.EventSourceId },
            { WellKnownCommandContextKeys.EventStreamId, _eventStreamId }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, null);
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_with_concurrency_scope() => _eventLog.Received().AppendMany(
        Arg.Any<EventSourceId>(),
        Arg.Any<IEnumerable<object>>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Is<ConcurrencyScope?>(cs => cs != null && cs.EventStreamId == _eventStreamId));

    [EventStreamId("Monthly", concurrency: true)]
    class TestCommand
    {
        public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;
    }
}

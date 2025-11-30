// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_handling_with_metadata;

public class with_concurrency_on_event_stream_type : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    CommandResult _result;
    EventStreamType _eventStreamType;

    void Establish()
    {
        _event = new TestEvent("Single Event");
        _eventStreamType = "Onboarding";

        var command = new TestCommand { EventSourceId = EventSourceId.New() };
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, command.EventSourceId },
            { WellKnownCommandContextKeys.EventStreamType, _eventStreamType }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, null);
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _event);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_with_concurrency_scope() => _eventLog.Received().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Is<ConcurrencyScope?>(cs => cs != null && cs.EventStreamType == _eventStreamType));

    [EventStreamType("Onboarding", concurrency: true)]
    class TestCommand
    {
        public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;
    }
}

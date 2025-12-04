// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.given;

public class a_single_event_command_response_value_handler : Specification
{
    protected SingleEventCommandResponseValueHandler _handler;
    protected IEventLog _eventLog;
    protected IEventTypes _eventTypes;
    protected CommandContext _commandContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventTypes = Substitute.For<IEventTypes>();
        _handler = new SingleEventCommandResponseValueHandler(_eventLog, _eventTypes);

        _correlationId = Guid.NewGuid();
        var command = new TestCommand { EventSourceId = EventSourceId.New() };
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, command.EventSourceId }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, null);

        // Set up default successful append result - handle all optional parameters
        var successfulResult = AppendResult.Success(_correlationId, EventSequenceNumber.First);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<ConcurrencyScope?>()).Returns(successfulResult);
    }

    protected class TestCommand
    {
        public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;
    }

    protected record TestEvent(string Name);
    protected record AnotherTestEvent(int Value);
    protected record UnknownEvent(string Data);
}

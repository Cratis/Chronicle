// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

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
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, []);
    }

    protected class TestCommand
    {
        public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;
    }

    protected record TestEvent(string Name);
    protected record AnotherTestEvent(int Value);
    protected record UnknownEvent(string Data);
}

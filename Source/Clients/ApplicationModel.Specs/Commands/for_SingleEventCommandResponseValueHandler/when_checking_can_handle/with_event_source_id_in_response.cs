// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_checking_can_handle;

public class with_event_source_id_in_response : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    bool _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _event = new TestEvent("Test Event");
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);

        _eventSourceId = EventSourceId.New();
        var command = new TestCommand();
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], new(), _eventSourceId);
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _event);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}

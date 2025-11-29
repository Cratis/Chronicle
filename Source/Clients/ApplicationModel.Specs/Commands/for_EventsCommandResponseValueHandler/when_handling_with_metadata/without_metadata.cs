// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_handling_with_metadata;

public class without_metadata : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    CommandResult _result;

    void Establish()
    {
        _events = [new TestEvent("Event without metadata")];

        var command = new TestCommand { EventSourceId = EventSourceId.New() };
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, command.EventSourceId }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, null);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_with_null_metadata() => _eventLog.Received().AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), null, null, null);
}

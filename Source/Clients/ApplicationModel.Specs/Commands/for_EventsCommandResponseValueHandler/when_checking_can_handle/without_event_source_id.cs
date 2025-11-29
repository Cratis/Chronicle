// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_checking_can_handle;

public class without_event_source_id : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    bool _result;
    CommandContext _commandContextWithoutEventSourceId;

    void Establish()
    {
        _events = [new TestEvent("Test")];
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);

        var commandWithoutEventSourceId = new CommandWithoutEventSourceId();
        _commandContextWithoutEventSourceId = new CommandContext(_correlationId, typeof(CommandWithoutEventSourceId), commandWithoutEventSourceId, [], new(), null);
    }

    void Because() => _result = _handler.CanHandle(_commandContextWithoutEventSourceId, _events);

    [Fact] void should_return_false() => _result.ShouldBeFalse();

    class CommandWithoutEventSourceId
    {
        public string SomeProperty { get; set; } = string.Empty;
    }
}

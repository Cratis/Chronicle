// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_checking_can_handle;

public class without_event_source_id : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    bool _result;

    void Establish()
    {
        _event = new TestEvent("Test Event");
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);

        var command = new TestCommandWithoutEventSourceId();
        _commandContext = new CommandContext(_correlationId, typeof(TestCommandWithoutEventSourceId), command, [], new(), null);
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _event);

    [Fact] void should_return_false() => _result.ShouldBeFalse();

    class TestCommandWithoutEventSourceId
    {
        public string Name { get; set; } = string.Empty;
    }
}

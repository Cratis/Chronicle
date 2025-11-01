// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_checking_can_handle;

public class with_unknown_event_type : given.a_single_event_command_response_value_handler
{
    UnknownEvent _event;
    bool _result;

    void Establish()
    {
        _event = new UnknownEvent("Unknown Data");
        _eventTypes.HasFor(typeof(UnknownEvent)).Returns(false);
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _event);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}

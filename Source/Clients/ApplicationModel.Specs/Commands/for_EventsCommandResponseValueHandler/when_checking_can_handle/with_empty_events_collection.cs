// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_checking_can_handle;

public class with_empty_events_collection : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    bool _result;

    void Establish()
    {
        _events = [];
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _events);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}

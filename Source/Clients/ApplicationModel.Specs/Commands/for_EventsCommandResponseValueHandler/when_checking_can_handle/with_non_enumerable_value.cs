// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_EventsCommandResponseValueHandler.when_checking_can_handle;

public class with_non_enumerable_value : given.an_events_command_response_value_handler
{
    string _value;
    bool _result;

    void Establish()
    {
        _value = "not an enumerable";
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _value);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}

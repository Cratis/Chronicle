// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_getting_event_source_type;

public class with_value : given.a_command_context
{
    EventSourceType _eventSourceType;
    EventSourceType? _result;

    void Establish()
    {
        _eventSourceType = new EventSourceType("Account");
        _commandContextValues[WellKnownCommandContextKeys.EventSourceType] = _eventSourceType;
    }

    void Because() => _result = _commandContext.GetEventSourceType();

    [Fact] void should_return_the_event_source_type() => _result.ShouldEqual(_eventSourceType);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_getting_event_stream_type;

public class with_value : given.a_command_context
{
    EventStreamType _eventStreamType;
    EventStreamType? _result;

    void Establish()
    {
        _eventStreamType = new EventStreamType("TestStream");
        _commandContextValues[WellKnownCommandContextKeys.EventStreamType] = _eventStreamType;
    }

    void Because() => _result = _commandContext.GetEventStreamType();

    [Fact] void should_return_the_event_stream_type() => _result.ShouldEqual(_eventStreamType);
}

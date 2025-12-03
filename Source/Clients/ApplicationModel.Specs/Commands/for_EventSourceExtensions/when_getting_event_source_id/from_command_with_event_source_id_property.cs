// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_command_with_event_source_id_property : Specification
{
    CommandWithEventSourceId _command;
    EventSourceId _eventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _command = new(_eventSourceId);
    }

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_the_event_source_id() => _result.ShouldEqual(_eventSourceId);

    record CommandWithEventSourceId(EventSourceId EventSourceId);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_command_with_multiple_event_source_id_properties : Specification
{
    CommandWithMultipleEventSourceIds _command;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();
        _command = new(_firstEventSourceId, _secondEventSourceId);
    }

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_the_first_event_source_id() => _result.ShouldEqual(_firstEventSourceId);

    record CommandWithMultipleEventSourceIds(EventSourceId FirstId, EventSourceId SecondId);
}

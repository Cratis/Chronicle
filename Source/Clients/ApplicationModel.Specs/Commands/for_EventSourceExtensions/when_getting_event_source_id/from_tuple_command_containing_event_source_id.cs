// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_tuple_command_containing_event_source_id : Specification
{
    ITuple _command;
    EventSourceId _eventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _command = (_eventSourceId, "some-data", 42);
    }

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_the_event_source_id() => _result.ShouldEqual(_eventSourceId);
}

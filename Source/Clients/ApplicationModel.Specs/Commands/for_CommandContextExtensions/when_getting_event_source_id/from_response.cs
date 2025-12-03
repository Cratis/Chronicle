// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_getting_event_source_id;

public class from_response : given.a_command_context
{
    EventSourceId _eventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _commandContext = _commandContext with { Response = _eventSourceId };
    }

    void Because() => _result = _commandContext.GetEventSourceId();

    [Fact] void should_return_the_event_source_id_from_response() => _result.ShouldEqual(_eventSourceId);
}

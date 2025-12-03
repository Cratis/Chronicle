// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_getting_event_stream_id;

public class with_value : given.a_command_context
{
    EventStreamId _eventStreamId;
    EventStreamId? _result;

    void Establish()
    {
        _eventStreamId = new EventStreamId(Guid.NewGuid().ToString());
        _commandContextValues[WellKnownCommandContextKeys.EventStreamId] = _eventStreamId;
    }

    void Because() => _result = _commandContext.GetEventStreamId();

    [Fact] void should_return_the_event_stream_id() => _result.ShouldEqual(_eventStreamId);
}

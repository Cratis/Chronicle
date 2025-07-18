// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_getting_appended_events : given.event_sequence_operations_without_any_operations
{
    EventSourceId _eventSourceId;
    object _appendedEvent;
    IEnumerable<object> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _appendedEvent = new object();
        _operations.ForEventSourceId(_eventSourceId, builder => builder.Append(_appendedEvent));
    }

    void Because() => _result = _operations.GetAppendedEvents();

    [Fact] void should_return_all_appended_events() => _result.ShouldContain(_appendedEvent);
}

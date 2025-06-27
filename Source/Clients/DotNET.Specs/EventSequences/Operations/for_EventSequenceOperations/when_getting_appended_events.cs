// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_getting_appended_events : given.event_sequence_operations_without_any_operations
{
    EventSourceId eventSourceId;
    object appendedEvent;
    IEnumerable<object> result;

    void Establish()
    {
        eventSourceId = EventSourceId.New();
        appendedEvent = new object();
        operations.ForEventSourceId(eventSourceId, builder => builder.Append(appendedEvent));
    }

    void Because() => result = operations.GetAppendedEvents();

    [Fact] void should_return_all_appended_events() => result.ShouldContain(appendedEvent);
}

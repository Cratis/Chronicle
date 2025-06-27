// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_clearing : given.event_sequence_operations_without_any_operations
{
    EventSourceId eventSourceId;
    object appendedEvent;

    void Establish()
    {
        eventSourceId = EventSourceId.New();
        appendedEvent = new object();
        operations
            .ForEventSourceId(eventSourceId, builder => builder.Append(appendedEvent))
            .WithCausation(CausationHelpers.New());
    }

    void Because() => operations.Clear();

    [Fact] void should_clear_all_event_source_builders() => operations.GetAppendedEvents().ShouldBeEmpty();
}

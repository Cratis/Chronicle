// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_clearing : given.event_sequence_operations_without_any_operations
{
    EventSourceId _eventSourceId;
    object _appendedEvent;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _appendedEvent = new object();
        _operations
            .ForEventSourceId(_eventSourceId, builder => builder.Append(_appendedEvent))
            .WithCausation(CausationHelpers.New());
    }

    void Because() => _operations.Clear();

    [Fact] void should_clear_all_event_source_builders() => _operations.GetAppendedEvents().ShouldBeEmpty();
}

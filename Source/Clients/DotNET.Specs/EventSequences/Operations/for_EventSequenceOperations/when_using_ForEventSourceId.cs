// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_using_ForEventSourceId : given.event_sequence_operations_without_any_operations
{
    EventSourceId eventSourceId;
    bool configured;

    void Establish()
    {
        eventSequence = Substitute.For<IEventSequence>();
        operations = new(eventSequence);
        eventSourceId = EventSourceId.New();
        configured = false;
    }

    void Because() => operations.ForEventSourceId(eventSourceId, _ => configured = true);

    [Fact] void should_add_and_configure_new_event_source_operations() => configured.ShouldBeTrue();
    [Fact] void should_return_self() => operations.ForEventSourceId(eventSourceId, _ => { }).ShouldEqual(operations);
}

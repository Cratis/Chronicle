// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.when_performing;

public class and_operation_has_own_causation : given.event_sequence_operations_without_any_operations
{
    EventSourceId eventSourceId;
    object appendedEvent;
    Causation causation;

    void Establish()
    {
        eventSequence = Substitute.For<IEventSequence>();
        operations = new(eventSequence);
        eventSourceId = EventSourceId.New();
        appendedEvent = new object();
        causation = CausationHelpers.New();
        operations.ForEventSourceId(eventSourceId, builder => builder.Append(appendedEvent, causation));
    }

    void Because() => operations.Perform().GetAwaiter().GetResult();

    [Fact] void should_use_operation_causation_for_event() => eventSequence.Received().AppendMany(Arg.Is<IEnumerable<EventForEventSourceId>>(events => events.All(e => e.Causation == causation)));
}

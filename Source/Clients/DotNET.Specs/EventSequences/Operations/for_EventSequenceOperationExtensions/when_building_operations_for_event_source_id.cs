// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperationExtensions;

public class when_building_operations_for_event_source_id : Specification
{
    IEventSequence event_sequence;
    EventSourceId event_source_id;
    Action<EventSourceOperations> configure;
    EventSequenceOperations result;
    EventSequenceOperations expected_operations;

    void Establish()
    {
        event_sequence = Substitute.For<IEventSequence>();
        event_source_id = Guid.NewGuid().ToString();
        configure = Substitute.For<Action<EventSourceOperations>>();
        expected_operations = Substitute.For<EventSequenceOperations>(event_sequence);
    }

    void Because() => result = event_sequence.ForEventSourceId(event_source_id, configure);

    [Fact] void should_return_event_sequence_operations() => result.ShouldNotBeNull();
    [Fact] void should_return_operations_for_the_given_event_sequence() => result.EventSequence.ShouldEqual(event_sequence);
}

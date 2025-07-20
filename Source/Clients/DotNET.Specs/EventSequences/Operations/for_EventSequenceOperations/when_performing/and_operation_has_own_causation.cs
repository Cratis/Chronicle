// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.when_performing;

public class and_operation_has_own_causation : given.event_sequence_operations_without_any_operations
{
    EventSourceId _eventSourceId;
    object _appendedEvent;
    Causation _causation;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _operations = new(_eventSequence);
        _eventSourceId = EventSourceId.New();
        _appendedEvent = new object();
        _causation = CausationHelpers.New();
        _operations.ForEventSourceId(_eventSourceId, builder => builder.Append(_appendedEvent, _causation));
    }

    Task Because() => _operations.Perform();

    [Fact]
    void should_use_operation_causation_for_event() => _eventSequence.Received().AppendMany(
        Arg.Is<IEnumerable<EventForEventSourceId>>(events => events.All(e => e.Causation == _causation)),
        concurrencyScopes: Arg.Any<Dictionary<EventSourceId, ConcurrencyScope>>());
}

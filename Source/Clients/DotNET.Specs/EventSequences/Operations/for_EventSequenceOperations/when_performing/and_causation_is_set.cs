// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.when_performing;

public class and_causation_is_set : given.event_sequence_operations_without_any_operations
{
    EventSourceId _eventSourceId;
    object _appendedEvent;
    Causation _causation;
    EventStreamType _eventStreamType = "TestStreamType";
    EventStreamId _eventStreamId = "TestStreamId";
    EventSourceType _eventSourceType = "TestSourceType";

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _operations = new(_eventSequence);
        _eventSourceId = EventSourceId.New();
        _appendedEvent = new object();
        _causation = CausationHelpers.New();
        _operations
            .ForEventSourceId(_eventSourceId, builder => builder
                .Append(
                    _appendedEvent,
                    eventStreamType: _eventStreamType,
                    eventStreamId: _eventStreamId,
                    eventSourceType: _eventSourceType))
            .WithCausation(_causation);
    }

    Task Because() => _operations.Perform();

    [Fact]
    void should_use_causation_for_event() => _eventSequence.Received().AppendMany(Arg.Is<IEnumerable<EventForEventSourceId>>(events => events.All(e =>
        e.Causation == _causation &&
        e.EventStreamType == _eventStreamType &&
        e.EventStreamId == _eventStreamId &&
        e.EventSourceType == _eventSourceType)),
        concurrencyScopes: Arg.Any<Dictionary<EventSourceId, ConcurrencyScope>>());
}

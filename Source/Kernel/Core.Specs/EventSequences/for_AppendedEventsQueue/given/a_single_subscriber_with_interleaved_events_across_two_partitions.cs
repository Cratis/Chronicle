// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// Creates six events with a single event type across two partitions where sequence
/// numbers interleave: partition A gets seq 0, 1, 4, 5 and partition B gets seq 2, 3.
/// This reproduces the scenario where grouping by partition before delivering would
/// cause an observer to advance past lower-numbered events from the other partition.
/// </summary>
public class a_single_subscriber_with_interleaved_events_across_two_partitions : a_single_subscriber
{
    protected EventType _eventType = new("Some event", 1);
    protected EventSourceId _partitionA;
    protected EventSourceId _partitionB;
    protected List<AppendedEvent> _allEvents;

    void Establish()
    {
        _partitionA = Guid.NewGuid();
        _partitionB = Guid.NewGuid();

        _allEvents =
        [
            CreateEvent(0, _partitionA),
            CreateEvent(1, _partitionA),
            CreateEvent(2, _partitionB),
            CreateEvent(3, _partitionB),
            CreateEvent(4, _partitionA),
            CreateEvent(5, _partitionA),
        ];
    }

    protected override IEnumerable<EventType> EventTypes => [_eventType];

    static AppendedEvent CreateEvent(ulong sequenceNumber, EventSourceId eventSourceId) =>
        new(
            EventContext.Empty with
            {
                SequenceNumber = sequenceNumber,
                EventType = new("Some event", 1),
                EventSourceId = eventSourceId
            },
            new System.Dynamic.ExpandoObject());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_fragmented_backlog.given;

/// <summary>
/// A single subscriber with queue telemetry, plus a recorder of the spills to catch-up the queue reports.
/// </summary>
public class a_fragmented_backlog : for_AppendedEventsQueue.given.a_single_subscriber_with_queue_telemetry
{
    protected const int Limit = AppendedEventsQueue.MaxPartitionRunsPerLiveDispatch;

    protected for_AppendedEventsQueue.given.QueueMetricsRecorder _spillRecorder;

    void Establish()
    {
        _spillRecorder = new(for_AppendedEventsQueue.given.QueueMetricsRecorder.ObserversSpilled, _recordedMeasurements.ObserverId);
        _observer.CatchUp().Returns(Task.CompletedTask);
    }

    void Destroy()
    {
        _spillRecorder.Dispose();
        _queue.Dispose();
    }

    /// <summary>
    /// Creates events that alternate between two event sources, so every event is a partition run of its own.
    /// </summary>
    /// <param name="count">The number of events.</param>
    /// <param name="eventType">The <see cref="EventType"/> of every event.</param>
    /// <param name="first">The sequence number of the first event.</param>
    /// <returns>The events, in sequence order.</returns>
    protected static AppendedEvent[] Alternating(int count, EventType eventType, ulong first = 0UL) =>
        [.. Enumerable.Range(0, count).Select(index => EventOf(eventType, index % 2 == 0 ? "a" : "b", first + (ulong)index))];

    protected static AppendedEvent EventOf(EventType eventType, string eventSourceId, ulong sequenceNumber) => AppendedEvent.Empty() with
    {
        Context = EventContext.Empty with
        {
            EventType = eventType,
            EventSourceId = eventSourceId,
            SequenceNumber = sequenceNumber
        }
    };

    protected async Task EnqueueAndDeplete(IReadOnlyList<AppendedEvent> events)
    {
        await _queue.Enqueue(events);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    protected IEnumerable<ulong> HandledFirstSequenceNumbers(IObserver observer) => observer
        .ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(IObserver.Handle))
        .Select(call => ((IEnumerable<AppendedEvent>)call.GetArguments()[1]!).First().Context.SequenceNumber.Value);
}

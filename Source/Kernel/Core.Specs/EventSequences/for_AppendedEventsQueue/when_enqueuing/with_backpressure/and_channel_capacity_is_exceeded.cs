// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_backpressure;

public class and_channel_capacity_is_exceeded : given.all_dependencies
{
    /// <summary>Channel capacity of 1 means: 1 batch can sit in the channel while the handler is busy.
    /// With capacity=1: to block, we need the handler busy (batch 1) + channel full (batch 2)
    /// before attempting to write batch 3.</summary>
    const int ChannelCapacity = 1;

    readonly EventType _eventType = new("backpressure-event", 1);
    readonly TaskCompletionSource _blockObserver = new();
    AppendedEventsQueue _queue;
    Task _thirdEnqueueTask;
    bool _thirdEnqueueCompletedWhileChannelWasFull;

    async Task Establish()
    {
        var observerKey = new ObserverKey("blocking-observer", "store", "ns", "seq");
        var blockingObserver = Substitute.For<IObserver>();

        blockingObserver
            .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ => _blockObserver.Task);

        _grainFactory.GetGrain<IObserver>(observerKey).Returns(blockingObserver);

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Options.Create(new ChronicleOptions
            {
                Events = new Configuration.Events { QueueBoundedCapacity = ChannelCapacity }
            }),
            Substitute.For<ILogger<AppendedEventsQueue>>());

        await _queue.Subscribe(observerKey, [_eventType]);
    }

    async Task Because()
    {
        var eventSourceId = new EventSourceId("backpressure-partition");
        AppendedEvent MakeEvent() => AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = eventSourceId
            }
        };

        // Batch 1: the handler dequeues and blocks on the observer — handler is now busy.
        await _queue.Enqueue([MakeEvent()]);

        // Give the handler time to dequeue and enter the observer.Handle call.
        await Task.Delay(100);

        // Batch 2: handler is busy, but channel still has room (capacity=1). Completes immediately.
        await _queue.Enqueue([MakeEvent()]);

        // Now: handler is blocked AND channel holds 1 item (full). Batch 3 must block.
        _thirdEnqueueTask = _queue.Enqueue([MakeEvent()]);

        // Wait briefly — if backpressure works the task must still be pending.
        await Task.Delay(100);
        _thirdEnqueueCompletedWhileChannelWasFull = _thirdEnqueueTask.IsCompleted;

        // Release the blocked observer so the queue drains and the test finishes cleanly.
        _blockObserver.SetResult();
        await _thirdEnqueueTask;
    }

    [Fact] void should_not_complete_third_enqueue_while_channel_was_full() =>
        _thirdEnqueueCompletedWhileChannelWasFull.ShouldBeFalse();
}

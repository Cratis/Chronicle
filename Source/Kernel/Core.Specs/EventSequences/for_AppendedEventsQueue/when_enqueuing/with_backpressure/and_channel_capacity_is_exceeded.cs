// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_backpressure;

/// <summary>
/// The append path must never block on observer consumption. When the bounded channel is full, Enqueue spills the
/// subscribed observers to their catch-up path instead of waiting: the events are already durable in the log, and
/// catch-up recovers the exact missed range from each observer's persisted next-event-sequence-number by cursor. The
/// subscription is removed so the consumer cannot deliver a later batch that would advance the observer's cursor past
/// the skipped range — the observer re-subscribes once it has caught up.
/// </summary>
public class and_channel_capacity_is_exceeded : given.all_dependencies
{
    /// <summary>Channel capacity of 1 means: 1 batch can sit in the channel while the handler is busy.
    /// With capacity=1: the handler busy (batch 1) + channel full (batch 2) makes batch 3 overflow.</summary>
    const int ChannelCapacity = 1;

    readonly EventType _eventType = new("backpressure-event", 1);
    readonly TaskCompletionSource _blockObserver = new();
    ObserverKey _observerKey;
    IObserver _observer;
    AppendedEventsQueue _queue;
    Task _thirdEnqueueTask;
    bool _thirdEnqueueCompletedWhileChannelWasFull;

    async Task Establish()
    {
        _observerKey = new ObserverKey("blocking-observer", "store", "ns", "seq");
        _observer = Substitute.For<IObserver>();
        _observer
            .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ => _blockObserver.Task);
        _observer.CatchUp().Returns(Task.CompletedTask);
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            new ActivitySource<AppendedEventsQueue>(),
            Options.Create(new ChronicleOptions
            {
                Events = new Configuration.Events { QueueBoundedCapacity = ChannelCapacity }
            }),
            Substitute.For<ILogger<AppendedEventsQueue>>());

        await _queue.Subscribe(_observerKey, [_eventType]);
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

        // Batch 2: handler is busy, but the channel still has room (capacity=1). Completes immediately.
        await _queue.Enqueue([MakeEvent()]);

        // Batch 3: handler is blocked AND the channel is full. The append must not block — it spills to catch-up.
        _thirdEnqueueTask = _queue.Enqueue([MakeEvent()]);
        _thirdEnqueueCompletedWhileChannelWasFull = _thirdEnqueueTask.IsCompleted;

        // Release the blocked observer so the queue drains and the test finishes cleanly.
        _blockObserver.SetResult();
        await _thirdEnqueueTask;
    }

    [Fact] void should_complete_the_overflowing_enqueue_without_blocking() =>
        _thirdEnqueueCompletedWhileChannelWasFull.ShouldBeTrue();

    [Fact] void should_spill_the_subscribed_observer_to_catchup() =>
        _observer.Received().CatchUp();

    [Fact] async Task should_remove_the_subscription_so_no_later_batch_advances_past_the_gap() =>
        (await _queue.GetSubscriptions()).ShouldBeEmpty();
}

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

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_coalescing;

/// <summary>
/// When several batches for the same partition pile up in the channel, the handler drains and coalesces them into a
/// single dispatch. Coalescing must preserve per-partition sequence order — the merged batch is sorted by sequence
/// number before delivery — and must not reorder or drop events.
/// </summary>
public class and_multiple_batches_are_queued_for_one_partition : given.all_dependencies
{
    readonly EventType _eventType = new("coalesce-event", 1);
    readonly TaskCompletionSource _firstHandleEntered = new();
    readonly TaskCompletionSource _releaseFirstHandle = new();
    readonly List<IReadOnlyList<AppendedEvent>> _handleCalls = [];
    ObserverKey _observerKey;
    IObserver _observer;
    AppendedEventsQueue _queue;
    EventSourceId _partition;
    int _handleInvocations;

    async Task Establish()
    {
        _partition = new EventSourceId("coalesce-partition");
        _observerKey = new ObserverKey("coalesce-observer", "store", "ns", "seq");
        _observer = Substitute.For<IObserver>();
        _observer
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray();
                lock (_handleCalls)
                {
                    _handleCalls.Add(events);
                }

                // The first dispatch blocks here so the follow-up batches accumulate in the channel; the handler
                // then drains and coalesces them into a single second dispatch once released.
                if (Interlocked.Increment(ref _handleInvocations) == 1)
                {
                    _firstHandleEntered.SetResult();
                    _releaseFirstHandle.Task.GetAwaiter().GetResult();
                }
            });
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            new ActivitySource<AppendedEventsQueue>(),
            Options.Create(new ChronicleOptions()),
            Substitute.For<ILogger<AppendedEventsQueue>>());

        await _queue.Subscribe(_observerKey, [_eventType]);
    }

    async Task Because()
    {
        // Batch 1 is dispatched alone and blocks the handler inside Handle.
        await _queue.Enqueue([Event(0)]);
        await _firstHandleEntered.Task;

        // While the handler is blocked, three more batches pile up in the channel.
        await _queue.Enqueue([Event(1)]);
        await _queue.Enqueue([Event(2)]);
        await _queue.Enqueue([Event(3)]);

        // Releasing the first Handle lets the handler drain and coalesce batches for seq 1..3 into one dispatch.
        _releaseFirstHandle.SetResult();
        await _queue.AwaitQueueDepletion();
    }

    AppendedEvent Event(ulong sequenceNumber) => AppendedEvent.Empty() with
    {
        Context = EventContext.Empty with
        {
            EventType = _eventType,
            EventSourceId = _partition,
            SequenceNumber = sequenceNumber
        }
    };

    [Fact] void should_deliver_every_event() =>
        _handleCalls.SelectMany(call => call).Select(@event => (ulong)@event.Context.SequenceNumber).ShouldContainOnly([0UL, 1UL, 2UL, 3UL]);

    [Fact] void should_preserve_per_partition_sequence_order() =>
        _handleCalls
            .SelectMany(call => call)
            .Select(@event => (ulong)@event.Context.SequenceNumber)
            .ToArray()
            .SequenceEqual([0UL, 1UL, 2UL, 3UL])
            .ShouldBeTrue();

    [Fact] void should_coalesce_the_queued_batches_into_a_single_dispatch() =>
        _handleCalls.Count.ShouldEqual(2);

    [Fact] void should_deliver_the_coalesced_events_in_one_call() =>
        _handleCalls[1].Select(@event => (ulong)@event.Context.SequenceNumber).ShouldContainOnly([1UL, 2UL, 3UL]);
}

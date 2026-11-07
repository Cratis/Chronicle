// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Tasks;
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
    readonly TaskCompletionSource _startWorker = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<Task> _workers = [];
    readonly List<IReadOnlyList<AppendedEvent>> _handleCalls = [];
    ObserverKey _observerKey;
    IObserver _observer;
    AppendedEventsQueue _queue;
    EventSourceId _partition;

    async Task Establish()
    {
        // Replace the base substitute so its eager Run callback cannot start an ungated worker.
        _taskFactory = Substitute.For<ITaskFactory>();
        _taskFactory.Run(Arg.Any<Func<Task>>()).Returns(callInfo =>
        {
            var worker = RunWorker(callInfo.Arg<Func<Task>>());
            lock (_workers)
            {
                _workers.Add(worker);
            }

            return worker;
        });

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
                    _delivered.TrySetResult();
                }
            });
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Options.Create(new ChronicleOptions()),
            Substitute.For<ILogger<AppendedEventsQueue>>());

        await _queue.Subscribe(_observerKey, [_eventType]);
    }

    async Task Because()
    {
        try
        {
            // Hold the queue reader, not Handle: all four batches must be present before draining starts.
            await _queue.Enqueue([Event(0)]);
            await _queue.Enqueue([Event(1)]);
            await _queue.Enqueue([Event(2)]);
            await _queue.Enqueue([Event(3)]);

            _startWorker.TrySetResult();
            await _delivered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            await _queue.AwaitQueueDepletion().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
        finally
        {
            await StopWorkers();
        }
    }

    Task Destroy() => StopWorkers();

    async Task RunWorker(Func<Task> worker)
    {
        await _startWorker.Task;
        await worker();
    }

    async Task StopWorkers()
    {
        _startWorker.TrySetResult();
        _queue?.Dispose();
        Task[] workers;
        lock (_workers)
        {
            workers = [.. _workers];
        }

        await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
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

    [Fact]
    void should_deliver_every_event() =>
        _handleCalls.SelectMany(call => call).Select(@event => (ulong)@event.Context.SequenceNumber).ShouldContainOnly([0UL, 1UL, 2UL, 3UL]);

    [Fact]
    void should_preserve_per_partition_sequence_order() =>
        _handleCalls
            .SelectMany(call => call)
            .Select(@event => (ulong)@event.Context.SequenceNumber)
            .ToArray()
            .SequenceEqual([0UL, 1UL, 2UL, 3UL])
            .ShouldBeTrue();

    [Fact]
    void should_coalesce_the_queued_batches_into_a_single_dispatch() =>
        _handleCalls.Count.ShouldEqual(1);

    [Fact]
    void should_deliver_the_coalesced_events_in_one_call() =>
        _handleCalls.Single()
            .Select(@event => (ulong)@event.Context.SequenceNumber)
            .SequenceEqual([0UL, 1UL, 2UL, 3UL])
            .ShouldBeTrue();
}

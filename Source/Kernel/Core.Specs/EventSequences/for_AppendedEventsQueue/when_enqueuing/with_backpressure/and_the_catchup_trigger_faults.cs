// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable.given;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_backpressure;

public class and_the_catchup_trigger_faults : given.all_dependencies
{
    readonly EventType _eventType = new("faulting-catchup-event", 1);
    readonly TaskCompletionSource _blockObserver = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _enteredObserver = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _catchupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CatchupClock _clock = new();
    IObserver _observer;
    AppendedEventsQueue _queue;
    int _attempts;

    async Task Establish()
    {
        var observerKey = new ObserverKey("faulting-observer", "store", "ns", "seq");
        _observer = Substitute.For<IObserver>();
        _observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(_ =>
        {
            _enteredObserver.TrySetResult();
            return _blockObserver.Task;
        });
        _observer.CatchUp().Returns(_ =>
        {
            if (Interlocked.Increment(ref _attempts) < 4)
            {
                return Task.FromException(new InvalidOperationException("catch-up could not be started"));
            }
            _catchupStarted.TrySetResult();
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IObserver>(observerKey).Returns(_observer);
        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            new ActivitySource<AppendedEventsQueue>(),
            Options.Create(new ChronicleOptions
            {
                Events = new Configuration.Events { QueueBoundedCapacity = 1 }
            }),
            Substitute.For<ILogger<AppendedEventsQueue>>())
        {
            CatchupTimeProvider = _clock
        };
        await _queue.Subscribe(observerKey, [_eventType]);
    }

    async Task Because()
    {
        var @event = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "faulting-partition" }
        };
        await _queue.Enqueue([@event]);
        await _enteredObserver.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _queue.Enqueue([@event]);
        await _queue.Enqueue([@event]);
        await _clock.AdvanceNextDelay();
        await _clock.AdvanceNextDelay();
        await _clock.AdvanceNextDelay();
        await _catchupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _blockObserver.TrySetResult();
        await _queue.AwaitQueueDepletion(500, 10);
    }

    void Destroy()
    {
        _blockObserver.TrySetResult();
        _queue?.Dispose();
    }

    [Fact] void should_keep_retrying_beyond_the_old_three_attempt_limit() => _observer.Received(4).CatchUp();
}

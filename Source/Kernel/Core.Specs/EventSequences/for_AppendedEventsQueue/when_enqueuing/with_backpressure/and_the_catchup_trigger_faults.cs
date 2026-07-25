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
/// The whole no-loss guarantee of the spill rests on the catch-up actually starting. If the CatchUp trigger faults
/// (transport/job-subsystem failure), the queue must not swallow it: the failure is observed and the trigger is
/// retried a bounded number of times rather than silently stranding the spilled observer behind the gap.
/// </summary>
public class and_the_catchup_trigger_faults : given.all_dependencies
{
    /// <summary>Mirrors AppendedEventsQueue.MaxCatchupTriggerAttempts — the bounded number of catch-up start attempts.</summary>
    const int ExpectedAttempts = 3;
    const int ChannelCapacity = 1;

    readonly EventType _eventType = new("faulting-catchup-event", 1);
    readonly TaskCompletionSource _blockObserver = new();
    ObserverKey _observerKey;
    IObserver _observer;
    AppendedEventsQueue _queue;
    int _observedCatchupAttempts;

    async Task Establish()
    {
        _observerKey = new ObserverKey("faulting-observer", "store", "ns", "seq");
        _observer = Substitute.For<IObserver>();
        _observer
            .Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ => _blockObserver.Task);
        _observer.CatchUp().Returns(Task.FromException(new InvalidOperationException("catch-up could not be started")));
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
        var eventSourceId = new EventSourceId("faulting-partition");
        AppendedEvent MakeEvent() => AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = eventSourceId
            }
        };

        // Fill the channel so the third enqueue overflows and spills the observer to catch-up.
        await _queue.Enqueue([MakeEvent()]);
        await Task.Delay(100);
        await _queue.Enqueue([MakeEvent()]);
        await _queue.Enqueue([MakeEvent()]);

        // Wait for the background catch-up trigger to observe the fault and exhaust its bounded retries.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && CatchupAttempts() < ExpectedAttempts)
        {
            await Task.Delay(20);
        }

        _observedCatchupAttempts = CatchupAttempts();
        _blockObserver.SetResult();
    }

    int CatchupAttempts() =>
        _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.CatchUp));

    [Fact] void should_observe_the_fault_and_retry_the_bounded_number_of_times() =>
        _observedCatchupAttempts.ShouldEqual(ExpectedAttempts);
}

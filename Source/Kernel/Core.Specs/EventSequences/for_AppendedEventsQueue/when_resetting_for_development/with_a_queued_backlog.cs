// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Tasks;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_resetting_for_development;

public class with_a_queued_backlog : Specification
{
    readonly ObserverKey _observerKey = new("old-observer", "store", "ns", "log");
    readonly EventType _eventType = new("reset-event", 1);
    readonly TaskCompletionSource _workerFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Func<Task> _worker;
    AppendedEventsQueue _queue;
    IGrainFactory _grainFactory;
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _subscriptions;
    bool _resetCompletedBeforeWorker;

    async Task Establish()
    {
        var tasks = Substitute.For<ITaskFactory>();
        tasks.Run(Arg.Any<Func<Task>>()).Returns(call =>
        {
            _worker = call.Arg<Func<Task>>();
            return _workerFinished.Task;
        });
        _grainFactory = Substitute.For<IGrainFactory>();
        _queue = new(tasks, _grainFactory, Substitute.For<IMeter<AppendedEventsQueue>>(), new ActivitySource<AppendedEventsQueue>(), Options.Create(new ChronicleOptions()), NullLogger<AppendedEventsQueue>.Instance);
        await _queue.Subscribe(_observerKey, [_eventType]);
        for (var sequence = 0; sequence < 1000; sequence++)
        {
            await _queue.Enqueue([AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = _eventType, SequenceNumber = (ulong)sequence } }]);
        }
    }

    async Task Because()
    {
        var reset = _queue.ResetForDevelopment();
        _resetCompletedBeforeWorker = reset.IsCompleted;

        // Release the worker only after reset has sealed subscriptions. No elapsed-time assumption controls the backlog.
        await _worker().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _workerFinished.SetResult();
        await reset.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _queue.SubscribeToAllEventTypes(_observerKey);
        await _queue.Subscribe(_observerKey, [_eventType]);
        await _queue.Enqueue([AppendedEvent.Empty()]);
        _subscriptions = await _queue.GetSubscriptions();
    }

    void Destroy() => _queue.Dispose();

    [Fact] void should_wait_for_the_worker() => _resetCompletedBeforeWorker.ShouldBeFalse();
    [Fact] void should_clear_and_seal_subscriptions() => _subscriptions.ShouldBeEmpty();
    [Fact] void should_never_deliver_the_backlog() => _grainFactory.DidNotReceive().GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string?>());
    [Fact] async Task should_leave_no_pending_batches() => await _queue.AwaitQueueDepletion().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
}

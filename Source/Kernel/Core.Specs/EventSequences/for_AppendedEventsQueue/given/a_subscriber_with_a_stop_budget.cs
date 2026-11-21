// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// A queue with one subscribed observer and a shared stop budget the spec controls: the stop has not begun until the
/// spec says so, and the deadline passes only when the spec cancels it.
/// </summary>
public class a_subscriber_with_a_stop_budget : all_dependencies
{
    protected readonly EventType _eventType = new("Some event", 1);
    protected readonly CancellationTokenSource _deadline = new();
    protected readonly TaskCompletionSource _handleStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _caughtUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected ObserverKey _observerKey;
    protected IObserver _observer;
    protected AppendedEventsQueue _queue;
    bool _stopping;

    protected bool Stopping
    {
        get => Volatile.Read(ref _stopping);
        set => Volatile.Write(ref _stopping, value);
    }

    async Task Establish()
    {
        _observerKey = new ObserverKey("Some observer", "Some event store", "Some namespace", "Some event sequence");
        _observer = Substitute.For<IObserver>();
        _observer.CatchUp().Returns(_ =>
        {
            _caughtUp.TrySetResult();
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);
        var budget = Substitute.For<IShutdownDrainBudget>();
        budget.IsStopping.Returns(_ => Stopping);
        budget.Token.Returns(_deadline.Token);
        budget.Timeout.Returns(TimeSpan.FromSeconds(8));
        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<AppendedEventsQueue>.Instance,
            budget);
        await _queue.Subscribe(_observerKey, [_eventType]);
    }

    void Destroy()
    {
        _queue.Dispose();
        _deadline.Dispose();
    }

    protected AppendedEvent EventAt(ulong sequenceNumber) =>
        AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "source", SequenceNumber = sequenceNumber } };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Tasks;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_resetting_for_development;

public class with_in_flight_delivery : Specification
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendedEventsQueue _queue;
    IObserver _observer;
    bool _resetCompletedBeforeDelivery;

    async Task Establish()
    {
        var tasks = Substitute.For<ITaskFactory>();
        tasks.Run(Arg.Any<Func<Task>>()).Returns(call => Task.Run(call.Arg<Func<Task>>()));
        var grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(_ =>
        {
            _entered.TrySetResult();
            return _release.Task;
        });
        var observerKey = new ObserverKey("old-observer", "store", "ns", "log");
        grainFactory.GetGrain<IObserver>(observerKey).Returns(_observer);
        _queue = new(tasks, grainFactory, Substitute.For<IMeter<AppendedEventsQueue>>(), new ActivitySource<AppendedEventsQueue>(), Options.Create(new ChronicleOptions()), NullLogger<AppendedEventsQueue>.Instance);
        var eventType = new EventType("reset-event", 1);
        await _queue.Subscribe(observerKey, [eventType]);
        await _queue.Enqueue([AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = eventType } }]);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _queue.Enqueue([AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = eventType, SequenceNumber = 1 } }]);
    }

    async Task Because()
    {
        var reset = _queue.ResetForDevelopment();
        _resetCompletedBeforeDelivery = reset.IsCompleted;
        _release.SetResult();
        await reset.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    void Destroy()
    {
        _release.TrySetResult();
        _queue.Dispose();
    }

    [Fact] void should_wait_for_the_in_flight_delivery() => _resetCompletedBeforeDelivery.ShouldBeFalse();
    [Fact] async Task should_discard_the_later_batch() => await _observer.Received(1).Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_other_observers_share_the_queue : given.an_unreachable_observer
{
    IObserver _healthyObserver;

    async Task Establish()
    {
        var key = new ObserverKey("healthy", "store", "ns", "seq");
        _healthyObserver = Substitute.For<IObserver>();
        _observerProbe = identity => identity == key.ToString() ? _healthyObserver : _observer;
        await _queue.Subscribe(key, [_eventType]);
    }

    async Task Because()
    {
        await DispatchFirstEvent();
        await _queue.Enqueue([_laterEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    [Fact] void should_keep_delivering_to_the_healthy_observers() =>
        _healthyObserver.Received(2).Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_spill_the_healthy_observer() => _healthyObserver.DidNotReceive().CatchUp();
    [Fact] void should_stop_live_delivery_to_the_unreachable_observer() =>
        _observer.Received(1).Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_spill_the_unreachable_observer() => _observer.Received(1).CatchUp();
}

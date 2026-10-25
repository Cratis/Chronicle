// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_spilled_to_catch_up;

public class and_zero_work_settles_after_queue_removal : given.an_observer_settling_after_zero_work
{
    void Establish() => _queued = false;

    Task Because() => _observer.CaughtUp(_jobId, 0UL);

    [Fact] void should_rejoin_the_physical_queue() => _queued.ShouldBeTrue();
    [Fact] async Task should_keep_the_logical_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_preserve_backoff() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] async Task should_not_start_another_catch_up() =>
        await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact]
    async Task should_deliver_a_later_live_event()
    {
        // A queue dispatches only to observers still in its subscription set.
        if (_queued)
        {
            await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(EventType.Unknown, 1UL)]);
        }
        await _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    }
}

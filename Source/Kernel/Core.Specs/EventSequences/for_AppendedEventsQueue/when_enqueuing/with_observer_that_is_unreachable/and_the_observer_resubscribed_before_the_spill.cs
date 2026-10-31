// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_the_observer_resubscribed_before_the_spill : given.an_unreachable_observer
{
    AppendedEventsQueueObserverSubscription _newSubscription;

    void Establish() => _observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(async _ =>
    {
        await _queue.Subscribe(_observerKey, [_eventType]);
        _newSubscription = (await _queue.GetSubscriptions()).Single();
        throw new InvalidOperationException("old activation unreachable");
    });

    Task Because() => DispatchFirstEvent();

    [Fact] async Task should_keep_the_new_subscription() =>
        (await _queue.GetSubscriptions()).Single().ShouldBeSame(_newSubscription);
    [Fact] void should_not_trigger_catch_up_for_the_new_subscription() => _observer.DidNotReceive().CatchUp();
}

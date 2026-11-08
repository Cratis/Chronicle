// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_holding_back;

public class and_a_subscriber_observes_another_event_type : given.a_single_subscriber_with_an_event_type
{
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _whileHeld;

    async Task Because()
    {
        await _queue.HoldBack("import-1", [new EventTypeId("imported-type")]);
        _whileHeld = await _queue.GetSubscriptions();
        await _queue.ReleaseHold("import-1");
        await Task.Delay(50);
    }

    [Fact] void should_keep_it_live() => _whileHeld.Count.ShouldEqual(1);
    [Fact] async Task should_not_catch_it_up() => await _observer.DidNotReceive().CatchUp();
}

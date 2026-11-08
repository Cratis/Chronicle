// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_holding_back;

public class and_a_subscriber_subscribes_while_held : given.a_single_subscriber_with_an_event_type
{
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _whileHeld;

    void Establish() => _observer.CatchUp().Returns(Task.CompletedTask);

    async Task Because()
    {
        await _queue.Unsubscribe(_observerKey);
        await _queue.HoldBack("import-1", [_eventType.Id]);
        await _queue.Subscribe(_observerKey, [_eventType]);
        _whileHeld = await _queue.GetSubscriptions();
        await _queue.ReleaseHold("import-1");
        await Task.Delay(100);
    }

    [Fact] void should_hold_it_back_too() => _whileHeld.ShouldBeEmpty();
    [Fact] async Task should_catch_it_up_when_released() => await _observer.Received(1).CatchUp();
}

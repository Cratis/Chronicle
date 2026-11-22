// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// Of the observers that are due, only those with an event waiting are woken.
/// </summary>
public class when_some_of_many_observers_are_behind : given.a_queue_with_a_stored_durable_subscription
{
    async Task Establish()
    {
        _tails[_eventType.Id] = 10;
        SetCursor(_observerKey.ObserverId, 11);
        var behind = IdInQueue(0, "behind");
        Store(behind, StoredKind);
        ObserverFor(KeyOf(behind));
        SetCursor(behind, 7);
        _queue = CreateQueue();
        await _queue.SeedDormantSubscriptions();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_wake_only_the_one_that_is_behind() => _calls.ToArray().ShouldEqual(["watchdog"]);
    [Fact] void should_not_wake_the_one_that_is_up_to_date() => _observer.DidNotReceive().RunWatchdogOnce();
}

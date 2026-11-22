// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// The cost of a pass does not grow with the number of observers that are up to date: the cursors are read once and the
/// tails once per observed event type, however many observers are due.
/// </summary>
public class when_many_observers_are_due : given.a_queue_with_a_stored_durable_subscription
{
    const int Observers = 8;

    async Task Establish()
    {
        _tails[_eventType.Id] = 10;
        SetCursor(_observerKey.ObserverId, 11);
        for (var index = 0; index < Observers; index++)
        {
            var id = IdInQueue(0, $"another-{index}");
            Store(id, StoredKind);
            ObserverFor(KeyOf(id));
            SetCursor(id, 11);
        }

        _queue = CreateQueue();
        await _queue.SeedDormantSubscriptions();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_hold_all_of_them_as_dormant() => _queue.DormantCount.ShouldEqual(Observers + 1);
    [Fact] void should_read_the_cursors_once() => _lookups.Count(lookup => lookup == "cursors").ShouldEqual(1);
    [Fact] void should_look_up_the_tail_of_each_event_type_once_and_of_redactions() => _lookups.Count(lookup => lookup == "tail").ShouldEqual(2);
    [Fact] void should_wake_none_of_them() => _calls.ToArray().ShouldEqual([]);
}

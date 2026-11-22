// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// Looking at an observer does not count as waking it: the full wake comes once the full-wake interval has passed since
/// the observer went dormant, however many checks found it up to date in between.
/// </summary>
public class when_the_full_wake_is_not_due_yet : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        for (var interval = 0; interval < 3; interval++)
        {
            _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
            await _queue.RunDormantSafetyNet();
        }

        // The full wake is due 60% to 90% of the full-wake interval after the observer went dormant, so by now it has fired once.
        _clock.Advance(TimeSpan.FromSeconds(FullWakeSeconds - (3 * SafetyNetSeconds)));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_have_woken_it_exactly_once_over_the_full_interval() => _calls.ToArray().ShouldEqual(["watchdog"]);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An observer that was found up to date is looked at again one interval later, not on the next tick.
/// </summary>
public class when_the_observer_stays_up_to_date_for_another_interval : given.a_queue_with_a_stored_durable_subscription
{
    int _cursorReadsAfterFirstCheck;

    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
        _cursorReadsAfterFirstCheck = _lookups.Count(lookup => lookup == "cursors");
        await _queue.RunDormantSafetyNet();
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_have_checked_it_once_at_first() => _cursorReadsAfterFirstCheck.ShouldEqual(1);
    [Fact] void should_not_check_it_again_before_its_next_interval() => _lookups.Count(lookup => lookup == "cursors").ShouldEqual(2);
    [Fact] void should_still_not_wake_it() => _calls.ToArray().ShouldEqual([]);
}

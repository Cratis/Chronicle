// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// Setting the full wake to the safety net interval wakes every dormant observer at every interval, as before the check existed.
/// </summary>
public class when_the_full_wake_interval_is_the_safety_net_interval : given.a_queue_with_a_stored_durable_subscription
{
    protected override int FullWakeSeconds => 900;

    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_wake_the_observer_although_it_is_up_to_date() => _calls.ToArray().ShouldEqual(["watchdog"]);
}

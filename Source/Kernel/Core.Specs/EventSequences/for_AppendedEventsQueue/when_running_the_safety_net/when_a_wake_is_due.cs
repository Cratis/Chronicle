// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An event whose doorbell was lost never reaches the dormant observer; the safety net wakes it within one interval.
/// </summary>
public class when_a_wake_is_due : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_run_one_watchdog_pass_in_the_observer() => _calls.ToArray().ShouldEqual(["watchdog"]);
    [Fact] async Task should_keep_the_subscription() => (await _queue.GetSubscriptions()).Count.ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An observer that went the full-wake interval without a wake is woken whatever the comparison says, because what only a
/// running observer can see (a client gone unnoticed, a definition that moved) cannot be seen from its cursor.
/// </summary>
public class when_the_full_wake_is_due : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(FullWakeSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_run_one_watchdog_pass() => _calls.ToArray().ShouldEqual(["watchdog"]);
    [Fact] void should_not_need_the_comparison() => _lookups.ToArray().ShouldEqual([]);
}

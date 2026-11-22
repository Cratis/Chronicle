// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// A check that cannot tell errs toward waking: an observer with no persisted cursor is woken.
/// </summary>
public class when_the_cursor_cannot_be_compared : given.a_queue_with_a_stored_durable_subscription
{
    void Establish() => _tails[_eventType.Id] = 10;

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_run_one_watchdog_pass() => _calls.ToArray().ShouldEqual(["watchdog"]);
}

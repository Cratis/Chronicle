// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// A cursor that holds no sequence number says nothing about what the observer has seen, so the observer is woken.
/// </summary>
public class when_the_cursor_is_not_an_actual_sequence_number : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, EventSequenceNumber.Unavailable);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_run_one_watchdog_pass() => _calls.ToArray().ShouldEqual(["watchdog"]);
}

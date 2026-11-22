// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An observer whose cursor is past the last event of the types it observes is not activated; other event types do not count.
/// </summary>
public class when_the_observer_is_up_to_date : given.a_queue_with_a_stored_durable_subscription
{
    async Task Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
        _tails["some-other-event-type"] = 500;
        await Task.CompletedTask;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_not_run_a_watchdog_pass() => _calls.ToArray().ShouldEqual([]);
    [Fact] void should_not_activate_the_observer() => _observer.DidNotReceive().Ensure();
    [Fact] async Task should_keep_it_dormant() => (await _queue.IsDormant(_observerKey)).ShouldBeTrue();
}

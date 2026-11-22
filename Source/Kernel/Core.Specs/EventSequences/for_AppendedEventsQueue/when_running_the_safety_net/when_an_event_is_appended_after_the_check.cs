// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An event appended after an observer was found up to date reaches it by its doorbell, as it always does.
/// </summary>
public class when_an_event_is_appended_after_the_check : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
        await _queue.Enqueue([EventAt(11)]);
        await _queue.AwaitQueueDepletion(periodNum: 500);
    }

    [Fact] void should_wake_the_observer_by_the_event() => _calls.ToArray().ShouldContain("ensure");
    [Fact] void should_deliver_the_event() => _handledEvents.ShouldEqual(1);
}

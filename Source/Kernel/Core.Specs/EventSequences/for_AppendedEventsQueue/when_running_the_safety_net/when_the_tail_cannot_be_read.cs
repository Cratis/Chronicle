// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// When the tail cannot be read the observers are woken instead of being taken for up to date.
/// </summary>
public class when_the_tail_cannot_be_read : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _eventSequenceGrain.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns<Task<EventSequenceNumber>>(_ => throw new InvalidOperationException("The tail is gone"));
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_run_one_watchdog_pass() => _calls.ToArray().ShouldEqual(["watchdog"]);
}

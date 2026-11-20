// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

public class when_no_wake_is_due_yet : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds * AppendedEventsQueue.SafetyNetEarliest) - TimeSpan.FromSeconds(1));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_not_wake_the_observer() => _calls.ShouldBeEmpty();
}

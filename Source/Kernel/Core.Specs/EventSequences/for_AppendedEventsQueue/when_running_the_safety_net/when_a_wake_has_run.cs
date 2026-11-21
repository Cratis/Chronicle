// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

public class when_a_wake_has_run : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_wake_it_only_once_per_interval() => _calls.Count.ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

public class and_the_instance_is_behind : given.a_queue_with_an_unsubscribed_instance
{
    bool _rungResult;

    void Establish() => _tails[_eventType.Id] = 7;

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
        _rungResult = await WasRung();
    }

    [Fact] void should_ring_the_observer() => _rungResult.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An instance with nothing waiting after its cursor is not activated.
/// </summary>
public class and_the_instance_is_up_to_date : given.a_queue_with_an_unsubscribed_instance
{
    void Establish() => _tails[_eventType.Id] = 4;

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_not_ring_the_observer() => _calls.ShouldBeEmpty();
}

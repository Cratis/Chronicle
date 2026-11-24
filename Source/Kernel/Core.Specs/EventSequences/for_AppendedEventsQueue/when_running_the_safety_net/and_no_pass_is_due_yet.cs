// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

public class and_no_pass_is_due_yet : given.a_queue_with_an_unsubscribed_instance
{
    void Establish() => _tails[_eventType.Id] = 7;

    async Task Because() => await _queue.RunDormantSafetyNet();

    [Fact] void should_not_ring_the_observer() => _calls.ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues;

public class when_resetting_for_development : given.two_seeded_queues
{
    async Task Because()
    {
        await _queues.ResetForDevelopment();
        await _queues.Enqueue([EventOfType(subscribed_event_type.Id)]);
    }

    [Fact] void should_reset_every_queue() => _queueGrains.ShouldEachConformTo(queue => queue.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAppendedEventsQueue.ResetForDevelopment)) == 1);
    [Fact] void should_stop_routing_old_event_types() => _queueGrains.ShouldEachConformTo(queue => !queue.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IAppendedEventsQueue.Enqueue)));
}

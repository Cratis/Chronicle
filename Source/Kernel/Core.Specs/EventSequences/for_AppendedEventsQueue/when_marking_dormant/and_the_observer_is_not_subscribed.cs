// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_marking_dormant;

public class and_the_observer_is_not_subscribed : given.a_queue_with_an_unsubscribed_instance
{
    bool _released;

    async Task Because() => _released = await _queue.TryMarkDormant(_observerKey);

    [Fact] void should_refuse() => _released.ShouldBeFalse();
}

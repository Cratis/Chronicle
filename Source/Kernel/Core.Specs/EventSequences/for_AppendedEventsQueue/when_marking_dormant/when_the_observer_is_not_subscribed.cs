// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_marking_dormant;

public class when_the_observer_is_not_subscribed : given.a_queue_with_a_stored_durable_subscription
{
    bool _result;

    async Task Because() => _result = await _queue.TryMarkDormant(KeyOf("never-subscribed"));

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}

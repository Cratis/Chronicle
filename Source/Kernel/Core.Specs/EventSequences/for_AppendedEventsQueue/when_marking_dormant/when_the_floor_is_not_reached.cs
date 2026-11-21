// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_marking_dormant;

public class when_the_floor_is_not_reached : given.a_queue_with_a_stored_durable_subscription
{
    protected override bool FloorIsReached => false;

    bool _result;

    async Task Establish() => await _queue.Subscribe(KeyOf("another"), [_eventType]);

    async Task Because() => _result = await _queue.TryMarkDormant(KeyOf("another"));

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_asking_whether_an_observer_is_dormant;

public class and_it_subscribes_again : given.a_queue_with_a_stored_durable_subscription
{
    bool _dormant;

    async Task Establish()
    {
        await _queue.Subscribe(KeyOf("woken"), [_eventType]);
        await _queue.TryMarkDormant(KeyOf("woken"));
        await _queue.Subscribe(KeyOf("woken"), [_eventType]);
    }

    async Task Because() => _dormant = await _queue.IsDormant(KeyOf("woken"));

    [Fact] void should_no_longer_be_dormant() => _dormant.ShouldBeFalse();
}

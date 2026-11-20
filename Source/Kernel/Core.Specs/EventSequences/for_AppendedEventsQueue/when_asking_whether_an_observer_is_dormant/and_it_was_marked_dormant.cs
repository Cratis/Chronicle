// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_asking_whether_an_observer_is_dormant;

public class and_it_was_marked_dormant : given.a_queue_with_a_stored_durable_subscription
{
    bool _dormant;
    bool _resident;

    async Task Establish()
    {
        await _queue.Subscribe(KeyOf("released"), [_eventType]);
        await _queue.Subscribe(KeyOf("resident"), [_eventType]);
        await _queue.TryMarkDormant(KeyOf("released"));
    }

    async Task Because()
    {
        _dormant = await _queue.IsDormant(KeyOf("released"));
        _resident = await _queue.IsDormant(KeyOf("resident"));
    }

    [Fact] void should_report_the_released_observer_as_dormant() => _dormant.ShouldBeTrue();
    [Fact] void should_not_report_the_resident_observer_as_dormant() => _resident.ShouldBeFalse();
}

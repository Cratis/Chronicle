// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

/// <summary>
/// An event appended after the tail was read, while the observer reads and delivers, reaches it by the queue and is delivered once.
/// </summary>
public class when_events_are_appended_while_waking : given.a_dormant_client_observer
{
    void Establish() => Append(EventAt(0), EventAt(1));

    async Task Because()
    {
        // The observer reads the tail and the first two events; this event is appended while it delivers them, so the doorbell
        // that follows carries it alone, and a second one carries an event the observer has already delivered.
        await Wake();
        Append(EventAt(2));
        await RingDoorbell("partition", EventAt(1));
        await RingDoorbell("partition", EventAt(2));
    }

    [Fact] void should_deliver_each_event_once() => _delivered.SelectMany(_ => _.Events).Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 1UL, 2UL]);
    [Fact] void should_deliver_in_order() => _delivered.SelectMany(_ => _.Events).Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 1UL, 2UL]);
    [Fact] void should_move_the_cursor_past_the_last_event() => _stateStorage.State.NextEventSequenceNumber.Value.ShouldEqual(3UL);
}

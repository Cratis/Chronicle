// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_an_unsubscribed_instance;

/// <summary>
/// An observer that has never run in the namespace is not activated there by an event: starting it is up to its registration.
/// </summary>
public class and_the_observer_has_no_instance_in_the_namespace : given.a_queue_with_an_unsubscribed_instance
{
    async Task Establish()
    {
        _cursors.Clear();
        await _queue.ReadInstances();
    }

    async Task Because() => await Deliver(EventAt(10));

    [Fact] void should_not_ring_the_observer() => _calls.ShouldBeEmpty();
}

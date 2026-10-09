// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_observer_that_is_not_an_inbox : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => ObservedSequence = EventSequenceId.Log;

    async Task Because() => _result = await Observe(Incoming(1, "ShipmentDispatched", "pending"), Incoming(2, "ShipmentDelivered", "dispatched"));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_say_it_is_not_an_inbox() => _result.ExceptionMessages.Any(_ => _.Contains("is not an inbox")).ShouldBeTrue();
    [Fact] void should_not_append_anything() => _appendCalls.ShouldEqual(0);
    [Fact] void should_not_remember_any_state() => _observations.ShouldBeEmpty();
}

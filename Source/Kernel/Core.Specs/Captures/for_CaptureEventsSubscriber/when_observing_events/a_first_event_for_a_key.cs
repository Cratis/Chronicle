// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class a_first_event_for_a_key : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await Observe(Incoming(1, "ShipmentDispatched", "pending"));

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_event_as_handled() => _result.LastSuccessfulObservation.ShouldEqual(new EventSequenceNumber(1));
    [Fact] void should_not_append_anything() => _appendCalls.ShouldEqual(0);
    [Fact] void should_remember_the_state_of_the_key() => _observations.Values.Single().Items.Single().Key.ShouldEqual("shipment-1");
}

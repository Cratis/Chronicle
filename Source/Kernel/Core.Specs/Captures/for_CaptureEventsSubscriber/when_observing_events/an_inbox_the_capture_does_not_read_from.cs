// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_inbox_the_capture_does_not_read_from : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => ObservedSequence = "inbox-billing";

    async Task Because() => _result = await Observe(Incoming(1, "ShipmentDispatched", "pending"));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_remember_any_state() => _observations.ShouldBeEmpty();
}

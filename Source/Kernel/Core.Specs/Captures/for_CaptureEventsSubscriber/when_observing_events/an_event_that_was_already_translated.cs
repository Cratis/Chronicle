// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_event_that_was_already_translated : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        await Observe(Incoming(1, "ShipmentDispatched", "pending"));
        _alreadyTagged = 1;
        _result = await Observe(Incoming(2, "ShipmentDelivered", "dispatched"));
    }

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_append_the_events_again() => _appendCalls.ShouldEqual(0);
    [Fact] void should_still_remember_the_state() => _observations.Values.Single().Items.Single().Content.ShouldContain("dispatched");
}

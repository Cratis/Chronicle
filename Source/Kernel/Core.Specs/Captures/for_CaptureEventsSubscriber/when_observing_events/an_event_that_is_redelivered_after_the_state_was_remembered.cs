// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_event_that_is_redelivered_after_the_state_was_remembered : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        var delivery = Incoming(2, "ShipmentDelivered", "dispatched");
        await Observe(Incoming(1, "ShipmentDispatched", "pending"), delivery);
        _result = await Observe(delivery);
    }

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_only_have_appended_once() => _appendCalls.ShouldEqual(1);
}

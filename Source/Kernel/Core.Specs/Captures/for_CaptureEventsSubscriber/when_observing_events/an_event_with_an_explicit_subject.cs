// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_event_with_an_explicit_subject : given.a_capture_events_subscriber
{
    async Task Because() => await Observe(
        Incoming(1, "ShipmentDispatched", "pending"),
        Incoming(2, "ShipmentDelivered", "dispatched", subject: new Subject("person-7")));

    [Fact] void should_carry_the_subject_to_the_private_event() => _appended[0].Subject!.Value.ShouldEqual("person-7");
}

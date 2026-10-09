// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class events_for_different_keys : given.a_capture_events_subscriber
{
    async Task Because() => await Observe(
        Incoming(1, "ShipmentDispatched", "pending", eventSourceId: "shipment-1"),
        Incoming(2, "ShipmentDispatched", "pending", eventSourceId: "shipment-2"),
        Incoming(3, "ShipmentDelivered", "dispatched", eventSourceId: "shipment-2"));

    [Fact] void should_only_append_for_the_key_that_changed() => _appended.Select(_ => _.EventSourceId.Value).ShouldContainOnly("shipment-2");
    [Fact] void should_remember_both_keys() => _observations.Values.Single().Items.Select(_ => _.Key).ShouldContainOnly("shipment-1", "shipment-2");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_subscribing;

public class with_a_type_from_another_origin : given.a_capture_events_subscriptions
{
    Exception _exception;

    void Establish() => RegisterPublic("ShipmentDelivered", "billing");

    async Task Because() => _exception = await Catch.Exception(() => _subscriptions.Subscribe(_eventStore, _definition));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<UnsupportedCaptureCapability>();
    [Fact] void should_not_subscribe_anywhere() => _observersByNamespace.ShouldBeEmpty();
}

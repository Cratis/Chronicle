// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class a_namespace_of_its_own : given.a_capture_events_subscriber
{
    CaptureId _expected;

    async Task Because()
    {
        _expected = CaptureObservers.ObservationIdFor(_capture.Id, _namespace);
        await Observe(Incoming(1, "ShipmentDispatched", "pending"));
    }

    [Fact] void should_remember_the_state_under_the_namespace_scoped_identity() => _observations.Keys.ShouldContainOnly(_expected);
    [Fact] void should_not_share_state_with_another_namespace() => _observations.ContainsKey(CaptureObservers.ObservationIdFor(_capture.Id, "tenant-b")).ShouldBeFalse();
}

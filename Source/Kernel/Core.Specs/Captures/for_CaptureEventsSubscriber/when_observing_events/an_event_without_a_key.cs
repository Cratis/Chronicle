// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_event_without_a_key : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await Observe(Incoming(1, "ShipmentDispatched", "pending", eventSourceId: string.Empty));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_report_it_as_handled() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}

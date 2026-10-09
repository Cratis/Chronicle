// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_append_that_fails : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        _appendResult = new AppendManyResult { Errors = [new AppendError("boom")] };
        _result = await Observe(
            Incoming(1, "ShipmentDispatched", "pending"),
            Incoming(2, "ShipmentDelivered", "dispatched"),
            Incoming(3, "ShipmentDelivered", "delivered"));
    }

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_report_the_reason() => _result.ExceptionMessages.Any(_ => _.Contains("boom")).ShouldBeTrue();
    [Fact] void should_only_report_the_events_before_it_as_handled() => _result.LastSuccessfulObservation.ShouldEqual(new EventSequenceNumber(1));
    [Fact] void should_not_continue_with_later_events() => _appendCalls.ShouldEqual(1);
    [Fact] void should_not_remember_the_state_of_the_failed_event() => _observations.Values.Single().Items.Single().Content.ShouldContain("pending");
}

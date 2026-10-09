// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_event_that_changes_the_state : given.a_capture_events_subscriber
{
    static readonly DateTimeOffset _sourceOccurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    CorrelationId _sourceCorrelation;
    ObserverSubscriberResult _result;

    async Task Because()
    {
        _sourceCorrelation = CorrelationId.New();
        _result = await Observe(
            Incoming(1, "ShipmentDispatched", "pending"),
            Incoming(2, "ShipmentDelivered", "dispatched", correlationId: _sourceCorrelation, occurred: _sourceOccurred));
    }

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_last_event_as_handled() => _result.LastSuccessfulObservation.ShouldEqual(new EventSequenceNumber(2));
    [Fact] void should_append_one_private_event() => _appended.Count.ShouldEqual(1);
    [Fact] void should_append_the_private_event_type() => _appended[0].EventType.Id.Value.ShouldEqual("OrderShipped");
    [Fact] void should_append_on_the_key_as_event_source() => _appended[0].EventSourceId.Value.ShouldEqual("shipment-1");
    [Fact] void should_map_the_incoming_event_content() => _appended[0].Content["orderId"]!.ToString().ShouldEqual("order-1");
    [Fact] void should_map_the_incoming_event_context() => _appended[0].Content["correlation"]!.ToString().ShouldEqual(_sourceCorrelation.ToString());
    [Fact] void should_record_the_private_event_as_a_local_fact_occurring_now() => _appended[0].Occurred.ShouldBeNull();
    [Fact] void should_keep_the_source_correlation() => _appendCorrelation.ShouldEqual(_sourceCorrelation);
    [Fact] void should_tag_the_event_with_the_capture() => _appended[0].Tags.ShouldContain(CaptureTags.Capture);
    [Fact] void should_tag_the_event_with_the_incoming_event() => _appended[0].Tags.ShouldContain(CaptureTags.ForSourceEvent(_capture.Id, _sequence, new EventSequenceNumber(2)));
    [Fact] void should_point_the_causation_at_the_inbox_event() => _appendCausation.Single().Properties[CaptureCausation.SourceSequenceNumber].ShouldEqual("2");
    [Fact] void should_point_the_causation_at_the_inbox() => _appendCausation.Single().Properties[CaptureCausation.SourceSequence].ShouldEqual("inbox-fulfillment");
    [Fact] void should_keep_the_source_occurrence_in_the_causation() => _appendCausation.Single().Properties[CaptureCausation.SourceOccurred].ShouldEqual(_sourceOccurred.ToString("O"));
    [Fact] void should_not_leak_the_default_subject() => _appended[0].Subject.ShouldBeNull();
}

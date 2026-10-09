// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class an_events_capture : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture ShipmentTracking
          source events
            sequence inbox-fulfillment
            from ShipmentDispatched
            from ShipmentDelivered
          key $eventSourceId
          append OrderShipped
            when status from "pending" to "dispatched"
              orderId = $.orderId
              shippedBy = $context.correlationId
        """;

    CaptureDefinition _result;

    void Because() => _result = Compile(Declaration);

    [Fact] void should_have_events_source() => _result.Source.Type.ShouldEqual(SourceType.Events);
    [Fact] void should_have_the_inbox_sequence() => _result.Source.Sequence.ShouldEqual("inbox-fulfillment");
    [Fact] void should_capture_from_every_event() => _result.Source.Events.ShouldContainOnly("ShipmentDispatched", "ShipmentDelivered");
    [Fact] void should_not_have_a_poll_interval() => _result.Source.Poll.ShouldBeNull();
    [Fact] void should_key_on_the_event_source_id() => _result.KeyProperty.ShouldEqual("$eventSourceId");
    [Fact] void should_append_the_private_event() => _result.Appends[0].EventType.ShouldEqual("OrderShipped");
    [Fact] void should_map_the_item_property() => _result.Appends[0].FieldAssignments["orderId"].ShouldEqual("$.orderId");
    [Fact] void should_map_the_event_context() => _result.Appends[0].FieldAssignments["shippedBy"].ShouldEqual("$context.correlationId");
}

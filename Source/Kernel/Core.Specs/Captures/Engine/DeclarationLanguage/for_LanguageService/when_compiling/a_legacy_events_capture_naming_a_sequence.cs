// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_legacy_events_capture_naming_a_sequence : given.a_language_service
{
    CaptureDefinition _result;

    void Because() => _result = Compile("""
        capture Shipments
          source events
            sequence inbox-fulfillment
            from ShipmentDispatched
            from ShipmentDelivered
          key $eventSourceId
          append OrderShipped
            when added
              id = $eventSourceId
        """);

    [Fact] void should_lift_the_inbox_sequence() => _result.Source.Sequence.ShouldEqual("inbox-fulfillment");
    [Fact] void should_keep_every_from_event() => _result.Source.Events.ShouldContainOnly("ShipmentDispatched", "ShipmentDelivered");
}

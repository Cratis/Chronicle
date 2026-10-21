// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_legacy_events_capture_with_non_identifier_event_ids : given.a_language_service
{
    CaptureDefinition _result;

    void Because() => _result = Compile("""
        capture Shipments
          source events
            sequence inbox-fulfillment
            from shipment-dispatched
            from 20ef254d-45e2-4f12-801a-6b00ef52423b
            from __chronicleEvent0
          key $eventSourceId
          append OrderShipped
            when added
              id = $eventSourceId
        """);

    [Fact] void should_keep_the_original_identifiers() => _result.Source.Events.ShouldContainOnly("shipment-dispatched", "20ef254d-45e2-4f12-801a-6b00ef52423b", "__chronicleEvent0");
    [Fact] void should_keep_the_legacy_sequence() => _result.Source.Sequence.ShouldEqual("inbox-fulfillment");
}

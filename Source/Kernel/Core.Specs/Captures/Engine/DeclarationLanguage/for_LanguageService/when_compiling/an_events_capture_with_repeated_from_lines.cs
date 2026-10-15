// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class an_events_capture_with_repeated_from_lines : given.a_language_service
{
    CaptureDefinition _result;

    void Because() => _result = Compile("""
        capture Shipments
          source events
            from ShipmentDispatched
            from ShipmentDispatched
            from ShipmentDispatched // duplicate
            from shipment-delivered
            from shipment-delivered
            from shipment-delivered # duplicate
          key $eventSourceId
        """);

    [Fact] void should_keep_the_first_from_for_each_event() => _result.Source.Events.ShouldContainOnly("ShipmentDispatched", "shipment-delivered");
}

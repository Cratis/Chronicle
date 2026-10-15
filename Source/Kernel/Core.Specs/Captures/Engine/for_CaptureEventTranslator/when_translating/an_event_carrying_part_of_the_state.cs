// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureEventTranslator.when_translating;

public class an_event_carrying_part_of_the_state : given.an_events_capture_translator
{
    CaptureEventTranslation _result;

    void Because() => _result = _translator.Translate(
        _definition,
        Content("dispatched"),
        new JsonObject { ["status"] = "delivered" },
        Context(sequenceNumber: 4));

    [Fact] void should_append_the_delivered_event() => _result.Events.Select(_ => _.Append.EventType).ShouldContainOnly("OrderDelivered");
    [Fact] void should_resolve_the_item_property_from_what_earlier_events_said() => _result.Events[0].Content["orderId"]!.ToString().ShouldEqual("order-1");
    [Fact] void should_resolve_the_event_source_id_expression() => _result.Events[0].Content["source"]!.ToString().ShouldEqual("shipment-1");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureEventTranslator.given;

public class an_events_capture_translator : Specification
{
    protected CaptureEventTranslator _translator;
    protected CaptureDefinition _definition;

    void Establish()
    {
        _translator = new(new CaptureChangeDetector(), new WhenClauseEvaluator(), new CaptureContentMapper());
        _definition = new(
            CaptureId.New(),
            "ShipmentTracking",
            new SourceDefinition(SourceType.Events, Sequence: "inbox-fulfillment", Events: ["ShipmentDispatched", "ShipmentDelivered"]),
            "$eventSourceId",
            null,
            [
                new AppendDefinition(
                    "OrderShipped",
                    new WhenClause(WhenClauseType.ValueTransition, ["status"], "pending", "dispatched"),
                    new Dictionary<string, string> { ["orderId"] = "$.orderId", ["correlation"] = "$context.correlationId" }),
                new AppendDefinition(
                    "OrderDelivered",
                    new WhenClause(WhenClauseType.ValueTransition, ["status"], "dispatched", "delivered"),
                    new Dictionary<string, string> { ["orderId"] = "$.orderId", ["source"] = "$eventSourceId" })
            ],
            [],
            []);
    }

    protected static JsonObject Content(string status) => new() { ["orderId"] = "order-1", ["status"] = status };

    protected static JsonObject Context(string eventSourceId = "shipment-1", ulong sequenceNumber = 1) => new()
    {
        ["eventType"] = "ShipmentDispatched",
        ["eventSourceId"] = eventSourceId,
        ["sequenceNumber"] = sequenceNumber,
        ["correlationId"] = "correlation-1"
    };
}

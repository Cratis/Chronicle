// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class an_events_capture_validator : given.a_capture_validator
{
    protected const string Dispatched = "ShipmentDispatched";
    protected const string Delivered = "ShipmentDelivered";
    protected const string Shipped = "OrderShipped";

    void Establish()
    {
        Register(Dispatched, EventTypeVisibility.Public, "fulfillment");
        Register(Delivered, EventTypeVisibility.Public, "fulfillment");
        Register(Shipped, EventTypeVisibility.Private, string.Empty);
    }

    protected void Register(string name, EventTypeVisibility visibility, string origin)
    {
        _eventTypes.HasFor(new EventTypeId(name)).Returns(true);
        _eventTypes.GetFor(new EventTypeId(name)).Returns(new EventTypeSchema(
            new EventType(name, EventTypeGeneration.First),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            new JsonSchema(),
            visibility,
            origin));
    }

    protected static CaptureDefinition EventsCapture(string? sequence = "inbox-fulfillment", string[]? from = null, string appends = Shipped, string? poll = null) =>
        CreateDefinition(
            new SourceDefinition(SourceType.Events, Poll: poll, Sequence: sequence, Events: from ?? [Dispatched, Delivered]),
            [new AppendDefinition(appends, new WhenClause(WhenClauseType.Added, []), new Dictionary<string, string> { ["orderId"] = "$eventSourceId", ["by"] = "$context.correlationId" })]);
}

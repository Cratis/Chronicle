// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamIndexUpdater.given;

public class a_closing_index : Specification
{
    protected ClosedStreamsConstraintStorage _storage;
    protected ClosesStreamConstraintDefinition _definition;
    protected readonly ClosedStreamScope _scope = new(EventSourceId: "source", EventStreamType: "transactions", EventStreamId: "month");

    void Establish()
    {
        _storage = new();
        _definition = new("closing", ["Closed"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId, ["Reopened"]);
    }

    protected ConstraintValidationContext ContextFor(EventTypeId eventType, ExpandoObject? content = null) =>
        new([], "source", eventType, content ?? new ExpandoObject(), EventSourceType.Default, "transactions", "month");
}

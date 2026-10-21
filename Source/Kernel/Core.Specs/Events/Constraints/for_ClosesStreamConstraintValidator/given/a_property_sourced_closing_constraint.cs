// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.given;

public class a_property_sourced_closing_constraint : Specification
{
    protected ClosesStreamConstraintDefinition _definition;
    protected ClosesStreamConstraintValidator _validator;
    protected ConstraintValidationContext _context;
    protected ExpandoObject _content;

    void Establish()
    {
        _definition = new("closing", ["Closed"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period");
        _validator = new(_definition, new ClosedStreamsConstraintStorage());
        _content = new();
        _context = new([], "source", "Closed", _content, EventSourceType.Default, EventStreamType.All, EventStreamId.Default);
    }
}

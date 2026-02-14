// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.given;

public class a_unique_event_type_constraint_validator_with_valid_definition : a_unique_event_type_constraint_validator
{
    protected ConstraintValidationContext _context;
    protected EventType _eventType = new("SomeEvent", 1);

    void Establish() => _context = new([], EventSourceId.New(), _eventType.Id, new());

    protected override UniqueEventTypeConstraintDefinition Definition => new("SomeConstraint", _eventType.Id);
}

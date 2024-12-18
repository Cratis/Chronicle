// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_asking_can_validate;

public class and_it_supports_event_type : given.a_unique_event_type_constraint_validator
{
    bool result;

    ConstraintValidationContext _context;

    EventType _eventType = new("SomeEvent", 1);


    void Establish() => _context = new([], EventSourceId.New(), _eventType.Id, new());

    protected override UniqueEventTypeConstraintDefinition Definition => new("SomeConstraint", _eventType.Id);

    void Because() => result = _validator.CanValidate(_context);

    [Fact] void should_be_able_to_validate() => result.ShouldBeTrue();
}

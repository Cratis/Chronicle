// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_asking_can_validate;

public class and_it_supports_event_type : given.a_unique_constraint_validator
{
    bool _result;

    ConstraintValidationContext _context;

    EventType _eventType = new("SomeEvent", 1);

    void Establish() => _context = new([], EventSourceId.New(), _eventType.Id, new());

    protected override UniqueConstraintDefinition Definition => new("SomeConstraint", [new(_eventType.Id, ["SomeProperty"])]);

    void Because() => _result = _validator.CanValidate(_context);

    [Fact] void should_be_able_to_validate() => _result.ShouldBeTrue();
}

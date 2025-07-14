// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_UniqueConstraintValidator.when_asking_can_validate;

public class and_it_does_not_support_event_type : given.a_unique_constraint_validator
{
    bool _result;

    ConstraintValidationContext _context;

    void Establish() => _context = new([], EventSourceId.New(), "SomeEvent", new());

    protected override UniqueConstraintDefinition Definition => new("SomeConstraint", [new("SomeOtherEvent", ["SomeProperty"])]);

    void Because() => _result = _validator.CanValidate(_context);

    [Fact] void should_not_be_able_to_validate() => _result.ShouldBeFalse();
}

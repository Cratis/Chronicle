// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext.when_validating;

public class multiple_validators_and_one_has_violation : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    ConstraintValidationResult _result;
    ConstraintViolation _violation;

    void Establish()
    {
        _firstValidator.Validate(Arg.Any<ConstraintValidationContext>()).Returns(new ConstraintValidationResult());

        _violation = new(_eventType.Id, 42, ConstraintType.Unknown, "The Constraint", "Something went wrong", []);
        _secondValidator.Validate(Arg.Any<ConstraintValidationContext>()).Returns(ConstraintValidationResult.Failed([_violation]));
    }

    async Task Because() => _result = await _context.Validate();

    [Fact] void should_call_the_first_validator() => _firstValidator.Received(1).Validate(_context);
    [Fact] void should_call_the_second_validator() => _secondValidator.Received(1).Validate(_context);
    [Fact] void should_have_one_violation() => _result.Violations.ShouldContainOnly(_violation);
}

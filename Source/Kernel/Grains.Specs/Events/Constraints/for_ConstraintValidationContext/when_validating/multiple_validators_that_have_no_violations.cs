// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext.when_validating;

public class multiple_validators_that_have_no_violations : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    ConstraintValidationResult _result;

    void Establish()
    {
        _firstValidator.Validate(Arg.Any<ConstraintValidationContext>()).Returns(new ConstraintValidationResult());
        _secondValidator.Validate(Arg.Any<ConstraintValidationContext>()).Returns(new ConstraintValidationResult());
    }

    async Task Because() => _result = await _context.Validate();

    [Fact] void should_call_the_first_validator() => _firstValidator.Received(1).Validate(_context);
    [Fact] void should_call_the_second_validator() => _secondValidator.Received(1).Validate(_context);
    [Fact] void should_have_no_violations() => _result.Violations.ShouldBeEmpty();
}

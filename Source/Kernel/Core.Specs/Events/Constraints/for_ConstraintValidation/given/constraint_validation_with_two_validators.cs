// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.given;

public class constraint_validation_with_two_validators : Specification
{
    protected IConstraintValidator _firstValidator;
    protected IConstraintValidator _secondValidator;
    protected ConstraintValidation _constraintValidation;

    void Establish()
    {
        _firstValidator = Substitute.For<IConstraintValidator>();
        _secondValidator = Substitute.For<IConstraintValidator>();

        _constraintValidation = new ConstraintValidation([_firstValidator, _secondValidator]);
    }
}

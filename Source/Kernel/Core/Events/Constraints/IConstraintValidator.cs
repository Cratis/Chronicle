// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that can validate constraints.
/// </summary>
public interface IConstraintValidator
{
    /// <summary>
    /// Gets the <see cref="IConstraintDefinition"/> for the validator.
    /// </summary>
    IConstraintDefinition Definition { get; }

    /// <summary>
    /// Check if the validator can validate a <see cref="ConstraintValidationContext"/> .
    /// </summary>
    /// <param name="context"><see cref="ConstraintValidationContext"/> to check.</param>
    /// <returns>True if it can validate, false if not.</returns>
    bool CanValidate(ConstraintValidationContext context);

    /// <summary>
    /// Perform validation of a <see cref="ConstraintValidationContext"/>.
    /// </summary>
    /// <param name="context"><see cref="ConstraintValidationContext"/> to validate.</param>
    /// <returns>The <see cref="ConstraintValidationResult"/> of the validation.</returns>
    Task<ConstraintValidationResult> Validate(ConstraintValidationContext context);
}

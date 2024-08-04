// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Defines a system that holds a set of <see cref="IConstraintValidator"/> that can validate constraints.
/// </summary>
public interface IConstraintValidatorSet
{
    /// <summary>
    /// Perform validation on a <see cref="EventToValidateForConstraints"/>.
    /// </summary>
    /// <param name="eventToValidate">The <see cref="EventToValidateForConstraints"/> to validate.</param>
    /// <returns><see cref="ConstraintValidationResult"/> holding the result of validation.</returns>
    Task<ConstraintValidationResult> Validate(EventToValidateForConstraints eventToValidate);

    /// <summary>
    /// Perform validation on a collection of <see cref="EventToValidateForConstraints"/>.
    /// </summary>
    /// <param name="eventsToValidate">The collection of <see cref="EventToValidateForConstraints"/> to validate.</param>
    /// <returns><see cref="ConstraintValidationResult"/> holding the result of validation.</returns>
    Task<ConstraintValidationResult> Validate(IEnumerable<EventToValidateForConstraints> eventsToValidate);
}

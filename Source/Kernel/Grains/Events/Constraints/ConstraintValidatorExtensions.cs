// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents extension methods for <see cref="IConstraintValidator"/>.
/// </summary>
public static class ConstraintValidatorExtensions
{
    /// <summary>
    /// Create a <see cref="ConstraintViolation"/> for a <see cref="IConstraintValidator"/>.
    /// </summary>
    /// <param name="validator"><see cref="IConstraintValidator"/> to create a violation for.</param>
    /// <param name="context"><see cref="ConstraintValidationContext"/> to create the violation from.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> of the existing event.</param>
    /// <param name="message"><see cref="ConstraintViolationMessage"/> to use.</param>
    /// <param name="details"><see cref="ConstraintViolationDetails"/> associated with the violation.</param>
    /// <returns>The created <see cref="ConstraintViolation"/>.</returns>
    public static ConstraintViolation CreateViolation(
        this IConstraintValidator validator,
        ConstraintValidationContext context,
        EventSequenceNumber sequenceNumber,
        ConstraintViolationMessage message,
        ConstraintViolationDetails? details = default)
    {
        details ??= [];
        return new(context.EventTypeId, sequenceNumber, validator.ToConstraintType(), validator.Definition.Name, message, details);
    }

    static ConstraintType ToConstraintType(this IConstraintValidator validator) =>
        validator switch
        {
            UniqueConstraintValidator => ConstraintType.Unique,
            UniqueEventTypeConstraintValidator => ConstraintType.UniqueEventType,
            _ => ConstraintType.Unknown
        };
}

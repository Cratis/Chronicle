// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints;

/// <summary>
/// Represents methods for converting between <see cref="ConstraintViolation"/> and <see cref="Contracts.Events.Constraints.ConstraintViolation"/>.
/// </summary>
internal static class ConstraintViolationConverters
{
    /// <summary>
    /// Convert to a Chronicle representation of <see cref="ConstraintViolation"/> to a contract version of <see cref="Contracts.Events.Constraints.ConstraintViolation"/>.
    /// </summary>
    /// <param name="violation"><see cref="ConstraintViolation"/> to convert.</param>
    /// <returns>A converted <see cref="Contracts.Events.Constraints.ConstraintViolation"/>.</returns>
    public static Contracts.Events.Constraints.ConstraintViolation ToContract(this ConstraintViolation violation) =>
        new()
        {
            EventTypeId = violation.EventTypeId,
            SequenceNumber = violation.SequenceNumber,
            ConstraintType = (Contracts.Events.Constraints.ConstraintType)violation.ConstraintType,
            ConstraintName = violation.ConstraintName,
            Message = violation.Message,
            Details = violation.Details
        };
}

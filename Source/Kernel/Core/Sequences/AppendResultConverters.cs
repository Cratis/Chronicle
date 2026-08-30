// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents methods for converting the violation types an append outcome carries to their contract
/// representations. The generator produces the surrounding <c>AppendResult</c>/<c>AppendManyResult</c> mapping
/// itself from their public properties; these are the element conversions it cannot derive on its own.
/// </summary>
internal static class AppendResultConverters
{
    /// <summary>
    /// Converts a <see cref="ConstraintViolation"/> to its <see cref="Contracts.Events.Constraints.ConstraintViolation"/> representation.
    /// </summary>
    /// <param name="violation">The <see cref="ConstraintViolation"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Events.Constraints.ConstraintViolation"/>.</returns>
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

    /// <summary>
    /// Converts a <see cref="ConcurrencyViolation"/> to its <see cref="Contracts.EventSequences.Concurrency.ConcurrencyViolation"/> representation.
    /// </summary>
    /// <param name="violation">The <see cref="ConcurrencyViolation"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.EventSequences.Concurrency.ConcurrencyViolation"/>.</returns>
    public static Contracts.EventSequences.Concurrency.ConcurrencyViolation ToContract(this ConcurrencyViolation violation) =>
        new()
        {
            EventSourceId = violation.EventSourceId,
            ExpectedSequenceNumber = violation.ExpectedSequenceNumber,
            ActualSequenceNumber = violation.ActualSequenceNumber
        };
}

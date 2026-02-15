// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency;

/// <summary>
/// Represents methods for converting between <see cref="ConcurrencyViolation"/> and <see cref="Contracts.EventSequences.Concurrency.ConcurrencyViolation"/>.
/// </summary>
internal static class ConcurrencyViolationConverters
{
    /// <summary>
    /// Convert to a Chronicle representation of <see cref="ConcurrencyViolation"/> to a contract version of <see cref="Contracts.Events.Constraints.ConstraintViolation"/>.
    /// </summary>
    /// <param name="violation"><see cref="ConcurrencyViolation"/> to convert.</param>
    /// <returns>A converted <see cref="Contracts.EventSequences.Concurrency.ConcurrencyViolation"/>.</returns>
    public static Contracts.EventSequences.Concurrency.ConcurrencyViolation ToContract(this ConcurrencyViolation violation) =>
        new()
        {
            ExpectedSequenceNumber = violation.ExpectedSequenceNumber,
            ActualSequenceNumber = violation.ActualSequenceNumber,
        };
}

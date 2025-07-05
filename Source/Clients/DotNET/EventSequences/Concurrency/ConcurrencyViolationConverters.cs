// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Converter methods for <see cref="ConcurrencyViolation"/>.
/// </summary>
internal static class ConcurrencyViolationConverters
{
    /// <summary>
    /// Convert to client version of <see cref="ConcurrencyViolation"/>.
    /// </summary>
    /// <param name="violation">Contract <see cref="Contracts.EventSequences.Concurrency.ConcurrencyViolation"/> to convert.</param>
    /// <returns>Converted client version.</returns>
    internal static ConcurrencyViolation ToClient(this Contracts.EventSequences.Concurrency.ConcurrencyViolation violation) =>
        new(
            EventSourceId: violation.EventSourceId,
            ExpectedEventSequenceNumber: (EventSequenceNumber)violation.ExpectedSequenceNumber,
            ActualEventSequenceNumber: (EventSequenceNumber)violation.ActualSequenceNumber);
}

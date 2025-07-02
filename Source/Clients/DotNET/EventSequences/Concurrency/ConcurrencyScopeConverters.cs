// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Converter methods for <see cref="ConcurrencyScope"/>.
/// </summary>
internal static class ConcurrencyScopeConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope"><see cref="ConcurrencyScope"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    internal static Contracts.EventSequences.Concurrency.ConcurrencyScope ToContract(this ConcurrencyScope scope) => new()
        {
            EventSequenceNumber = scope.SequenceNumber.Value,
            EventSourceId = scope.EventSourceId is not null,
            EventStreamType = scope.EventStreamType?.Value,
            EventStreamId = scope.EventStreamId?.Value,
            EventSourceType = scope.EventSourceType?.Value,
            EventTypes = scope.EventTypes?.Select(et => et.ToString()).ToList()
        };
}

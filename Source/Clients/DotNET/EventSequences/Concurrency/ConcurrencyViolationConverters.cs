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
            ActualEventSequenceNumber: (EventSequenceNumber)violation.ActualSequenceNumber)
        {
            ScopeIndex = violation.ScopeIndex,
            Scope = violation.Scope is null ? null : new ConcurrencyScope(
                violation.Scope.ExpectsNoMatchingEvent ? EventSequenceNumber.BeforeFirst : new EventSequenceNumber(violation.Scope.SequenceNumber),
                violation.Scope.EventSourceId ? new EventSourceId(violation.EventSourceId) : null,
                violation.Scope.EventStreamType is null ? null : new EventStreamType(violation.Scope.EventStreamType),
                violation.Scope.EventStreamId is null ? null : new EventStreamId(violation.Scope.EventStreamId),
                violation.Scope.EventSourceType is null ? null : new EventSourceType(violation.Scope.EventSourceType),
                violation.Scope.EventTypes?.Select(_ => _.ToClient()).ToArray()),
            ConflictingEventTypeId = string.IsNullOrEmpty(violation.ConflictingEventTypeId) ? null : new EventTypeId(violation.ConflictingEventTypeId),
            ConflictingEventSequenceNumber = violation.ConflictingSequenceNumber is { } conflicting ? new EventSequenceNumber(conflicting) : null
        };
}

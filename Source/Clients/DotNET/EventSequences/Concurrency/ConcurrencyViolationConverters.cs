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
            Scope = ToClient(violation.Scope, violation.EventSourceId),
            ConflictingEventTypeId = string.IsNullOrEmpty(violation.ConflictingEventTypeId) ? null : new EventTypeId(violation.ConflictingEventTypeId),
            ConflictingEventSequenceNumber = violation.ConflictingSequenceNumber is { } conflicting ? new EventSequenceNumber(conflicting) : null
        };

    static ConcurrencyScope? ToClient(Contracts.Sequences.ConcurrencyScope? scope, string eventSourceId)
    {
        if (scope is null)
        {
            return null;
        }

        return new ConcurrencyScope(
            scope.ExpectsNoMatchingEvent ? EventSequenceNumber.BeforeFirst : new EventSequenceNumber(scope.SequenceNumber),
            scope.EventSourceId ? new EventSourceId(eventSourceId) : null,
            scope.EventStreamType is null ? null : new EventStreamType(scope.EventStreamType),
            scope.EventStreamId is null ? null : new EventStreamId(scope.EventStreamId),
            scope.EventSourceType is null ? null : new EventSourceType(scope.EventSourceType),
            scope.EventTypes?.Select(_ => _.ToClient()).ToArray());
    }
}

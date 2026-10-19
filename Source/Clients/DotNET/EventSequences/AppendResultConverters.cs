// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents methods for converting between <see cref="Contracts.Sequences.AppendResponse"/> to <see cref="AppendResult"/>.
/// </summary>
internal static class AppendResultConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Sequences.AppendResponse"/> to <see cref="AppendResult"/>.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Sequences.AppendResponse"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendResult"/>.</returns>
    public static AppendResult ToClient(this Contracts.Sequences.AppendResponse result)
    {
        return new AppendResult
        {
            CorrelationId = result.CorrelationId,
            SequenceNumber = result.SequenceNumber,
            ConstraintViolations = (result.ConstraintViolations ?? []).Select(v => v.ToClient()).ToImmutableList(),
            Errors = (result.Errors ?? []).Select(e => (AppendError)e).ToImmutableList(),
            ConcurrencyViolation = result.ConcurrencyViolation?.ToClient(),
            ConcurrencyCheckPerformed = result.ConcurrencyCheckPerformed
        };
    }

    /// <summary>
    /// Convert from <see cref="Contracts.Sequences.AppendManyResponse"/> to <see cref="AppendManyResult"/>.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Sequences.AppendManyResponse"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendManyResult"/>.</returns>
    public static AppendManyResult ToClient(this Contracts.Sequences.AppendManyResponse result)
    {
        return new AppendManyResult
        {
            CorrelationId = result.CorrelationId,
            SequenceNumbers = result.SequenceNumbers.Select(_ => (EventSequenceNumber)_).ToImmutableList(),
            Outcomes = result.SequenceNumbers.Select(_ => AppendOutcome.Appended).ToImmutableList(),
            ConstraintViolations = (result.ConstraintViolations ?? []).Select(v => v.ToClient()).ToImmutableList(),
            Errors = (result.Errors ?? []).Select(e => (AppendError)e).ToImmutableList(),
            ConcurrencyViolations = (result.ConcurrencyViolations ?? []).Select(_ => _.ToClient()).ToImmutableList(),
            ConcurrencyCheckPerformed = result.ConcurrencyCheckPerformed
        };
    }

    /// <summary>
    /// Convert from <see cref="Contracts.Sequences.AppendOnceResponse"/> to <see cref="AppendResult"/>.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Sequences.AppendOnceResponse"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendResult"/>.</returns>
    public static AppendResult ToClient(this Contracts.Sequences.AppendOnceResponse result)
    {
        return new AppendResult
        {
            CorrelationId = result.CorrelationId,
            SequenceNumber = result.SequenceNumber,
            Outcome = result.Outcome.ToClient(),
            ConstraintViolations = (result.ConstraintViolations ?? []).Select(v => v.ToClient()).ToImmutableList(),
            Errors = (result.Errors ?? []).Select(e => (AppendError)e).ToImmutableList(),
            ConcurrencyViolation = result.ConcurrencyViolation?.ToClient(),
            ConcurrencyCheckPerformed = result.ConcurrencyCheckPerformed,
            IdempotencyConflicts = (result.IdempotencyConflicts ?? []).Select(_ => _.ToClient()).ToImmutableList()
        };
    }

    /// <summary>
    /// Convert from <see cref="Contracts.Sequences.AppendManyOnceResponse"/> to <see cref="AppendManyResult"/>.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Sequences.AppendManyOnceResponse"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendManyResult"/>.</returns>
    public static AppendManyResult ToClient(this Contracts.Sequences.AppendManyOnceResponse result)
    {
        return new AppendManyResult
        {
            CorrelationId = result.CorrelationId,
            SequenceNumbers = result.SequenceNumbers.Select(_ => (EventSequenceNumber)_).ToImmutableList(),
            Outcomes = (result.Outcomes ?? []).Select(_ => _.ToClient()).ToImmutableList(),
            ConstraintViolations = (result.ConstraintViolations ?? []).Select(v => v.ToClient()).ToImmutableList(),
            Errors = (result.Errors ?? []).Select(e => (AppendError)e).ToImmutableList(),
            ConcurrencyViolations = (result.ConcurrencyViolations ?? []).Select(_ => _.ToClient()).ToImmutableList(),
            ConcurrencyCheckPerformed = result.ConcurrencyCheckPerformed,
            IdempotencyConflicts = (result.IdempotencyConflicts ?? []).Select(_ => _.ToClient()).ToImmutableList()
        };
    }

    /// <summary>
    /// Convert from the contract <see cref="Contracts.EventSequences.AppendOutcome"/> to <see cref="AppendOutcome"/>.
    /// </summary>
    /// <param name="outcome">The contract outcome.</param>
    /// <returns>The client outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kernel returned an outcome this client does not know.</exception>
    public static AppendOutcome ToClient(this Contracts.EventSequences.AppendOutcome outcome) => outcome switch
    {
        Contracts.EventSequences.AppendOutcome.Committed => AppendOutcome.Appended,
        Contracts.EventSequences.AppendOutcome.AlreadyAppended => AppendOutcome.AlreadyAppended,
        Contracts.EventSequences.AppendOutcome.AlreadyAppendedAfterErasure => AppendOutcome.AlreadyAppendedAfterErasure,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "The kernel returned an unknown append outcome.")
    };

    /// <summary>
    /// Convert from the contract <see cref="Contracts.Sequences.IdempotencyKeyConflict"/> to <see cref="IdempotencyKeyConflict"/>.
    /// </summary>
    /// <param name="conflict">The contract conflict.</param>
    /// <returns>The client conflict.</returns>
    public static IdempotencyKeyConflict ToClient(this Contracts.Sequences.IdempotencyKeyConflict conflict)
    {
        // The kernel has already committed by now, so nothing here may throw: an unknown reason is unverifiable
        // and the key is carried exactly as received.
        var reason = conflict.Reason switch
        {
            Contracts.EventSequences.IdempotencyConflictReason.Content => IdempotencyConflictReason.Content,
            Contracts.EventSequences.IdempotencyConflictReason.Generation => IdempotencyConflictReason.Generation,
            _ => IdempotencyConflictReason.Unverifiable
        };
        var storedEventType = new EventType(conflict.StoredEventTypeId, conflict.StoredEventTypeGeneration);
        return new(IdempotencyKey.AsReceived(conflict.Key), conflict.ExistingSequenceNumber, storedEventType, conflict.RequestedGeneration, reason);
    }
}

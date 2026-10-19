// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the outcome of appending a single event once for an idempotency key.
/// </summary>
public class AppendOnceResult
{
    /// <summary>
    /// Gets the correlation identifier of the attempt.
    /// </summary>
    public CorrelationId CorrelationId { get; init; } = CorrelationId.NotSet;

    /// <summary>
    /// Gets the sequence number of the event, new or already stored.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; init; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets how the keyed append settled.
    /// </summary>
    public AppendOutcome Outcome { get; init; } = AppendOutcome.Appended;

    /// <summary>
    /// Gets a value indicating whether the append succeeded.
    /// </summary>
    public bool IsSuccess => !HasConstraintViolations && !HasConcurrencyViolations && !HasErrors;

    /// <summary>
    /// Gets a value indicating whether there are constraint violations.
    /// </summary>
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <summary>
    /// Gets a value indicating whether there is a concurrency violation.
    /// </summary>
    public bool HasConcurrencyViolations => ConcurrencyViolation is not null;

    /// <summary>
    /// Gets a value indicating whether there are errors.
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Gets a value indicating whether a concurrency check was performed.
    /// </summary>
    public bool ConcurrencyCheckPerformed { get; init; }

    /// <summary>
    /// Gets the constraint violations.
    /// </summary>
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets the errors, including refusals to settle the key.
    /// </summary>
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Gets the concurrency violation, if any.
    /// </summary>
    public ConcurrencyViolation? ConcurrencyViolation { get; init; }

    /// <summary>
    /// Gets the keys refused because they are already durable for a different intent.
    /// </summary>
    public IEnumerable<IdempotencyKeyConflict> IdempotencyConflicts { get; init; } = [];

    /// <summary>
    /// Creates the result from the grain's settlement.
    /// </summary>
    /// <param name="settlement">The <see cref="IdempotentAppendResult"/>.</param>
    /// <returns>The <see cref="AppendOnceResult"/>.</returns>
    internal static AppendOnceResult From(IdempotentAppendResult settlement) => new()
    {
        CorrelationId = settlement.Result.CorrelationId,
        SequenceNumber = settlement.Result.SequenceNumber,
        Outcome = settlement.Outcome,
        ConcurrencyCheckPerformed = settlement.Result.ConcurrencyCheckPerformed,
        ConstraintViolations = settlement.Result.ConstraintViolations,
        Errors = settlement.Result.Errors,
        ConcurrencyViolation = settlement.Result.ConcurrencyViolation,
        IdempotencyConflicts = settlement.Conflicts.Select(IdempotencyKeyConflict.From).ToArray()
    };
}

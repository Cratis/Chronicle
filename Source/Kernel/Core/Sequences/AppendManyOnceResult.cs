// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the outcome of appending a batch in which keyed events settle against their durable identities.
/// </summary>
public class AppendManyOnceResult
{
    /// <summary>
    /// Gets the correlation identifier of the attempt.
    /// </summary>
    public CorrelationId CorrelationId { get; init; } = CorrelationId.NotSet;

    /// <summary>
    /// Gets the sequence numbers in request order, new or already stored.
    /// </summary>
    public IEnumerable<EventSequenceNumber> SequenceNumbers { get; init; } = [];

    /// <summary>
    /// Gets how each event settled, aligned with the request events.
    /// </summary>
    public IEnumerable<AppendOutcome> Outcomes { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the append succeeded.
    /// </summary>
    public bool IsSuccess => !HasConstraintViolations && !HasConcurrencyViolations && !HasErrors;

    /// <summary>
    /// Gets a value indicating whether there are constraint violations.
    /// </summary>
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <summary>
    /// Gets a value indicating whether there are concurrency violations.
    /// </summary>
    public bool HasConcurrencyViolations => ConcurrencyViolations.Any();

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
    /// Gets the errors, including refusals to settle keys.
    /// </summary>
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Gets the concurrency violations.
    /// </summary>
    public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; init; } = [];

    /// <summary>
    /// Gets the keys refused because they are already durable for a different intent.
    /// </summary>
    public IEnumerable<IdempotencyKeyConflict> IdempotencyConflicts { get; init; } = [];

    /// <summary>
    /// Creates the result from the grain's settlement.
    /// </summary>
    /// <param name="settlement">The <see cref="IdempotentAppendManyResult"/>.</param>
    /// <returns>The <see cref="AppendManyOnceResult"/>.</returns>
    internal static AppendManyOnceResult From(IdempotentAppendManyResult settlement) => new()
    {
        CorrelationId = settlement.Result.CorrelationId,
        SequenceNumbers = settlement.Result.SequenceNumbers,
        Outcomes = settlement.Outcomes,
        ConcurrencyCheckPerformed = settlement.Result.ConcurrencyCheckPerformed,
        ConstraintViolations = settlement.Result.ConstraintViolations,
        Errors = settlement.Result.Errors,
        ConcurrencyViolations = settlement.Result.ConcurrencyViolations,
        IdempotencyConflicts = settlement.Conflicts.Select(IdempotencyKeyConflict.From).ToArray()
    };
}

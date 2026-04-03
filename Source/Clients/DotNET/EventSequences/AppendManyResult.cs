// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the result of an append many operation.
/// </summary>
public record AppendManyResult : IAppendResult
{
    /// <inheritdoc />
    public CorrelationId CorrelationId { get; init; } = CorrelationId.NotSet;

    /// <summary>
    /// Gets the sequence numbers of the events that were appended, if successful. In the same sequence as the events were provided.
    /// </summary>
    public IEnumerable<EventSequenceNumber> SequenceNumbers { get; init; } = [];

    /// <inheritdoc />
    public bool IsSuccess => !HasConstraintViolations && !HasErrors && !HasConcurrencyViolations;

    /// <inheritdoc />
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <inheritdoc />
    public bool HasConcurrencyViolations => ConcurrencyViolations.Any();

    /// <inheritdoc />
    public bool HasErrors => Errors.Any();

    /// <inheritdoc />
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets any concurrency violations that occurred during the operation.
    /// </summary>
    public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; init; } = [];

    /// <inheritdoc />
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Create a successful result.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="sequenceNumbers">Collection of <see cref="EventSequenceNumber"/> to report.</param>
    /// <returns>A new <see cref="AppendResult"/> instance.</returns>
    public static AppendManyResult Success(CorrelationId correlationId, IEnumerable<EventSequenceNumber> sequenceNumbers) => new()
    {
        CorrelationId = correlationId,
        SequenceNumbers = sequenceNumbers.ToImmutableList()
    };

    /// <summary>
    /// Create a failed result with constraint violations.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="violations">The violations.</param>
    /// <returns>A new failed <see cref="AppendManyResult"/> instance.</returns>
    public static AppendManyResult Failed(CorrelationId correlationId, IEnumerable<ConstraintViolation> violations) => new()
    {
        CorrelationId = correlationId,
        ConstraintViolations = violations.ToImmutableList()
    };

    /// <summary>
    /// Create a failed result with concurrency violations.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="violations">The <see cref="ConcurrencyViolation"/> instances that occurred.</param>
    /// <returns>A new failed <see cref="AppendManyResult"/> instance.</returns>
    public static AppendManyResult Failed(CorrelationId correlationId, IEnumerable<ConcurrencyViolation> violations) => new()
    {
        CorrelationId = correlationId,
        ConcurrencyViolations = violations.ToImmutableList()
    };

    /// <summary>
    /// Create a failed result with errors.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="errors">The errors.</param>
    /// <returns>A new failed <see cref="AppendManyResult"/> instance.</returns>
    public static AppendManyResult Failed(CorrelationId correlationId, IEnumerable<AppendError> errors) => new()
    {
        CorrelationId = correlationId,
        Errors = errors.ToImmutableList()
    };
}

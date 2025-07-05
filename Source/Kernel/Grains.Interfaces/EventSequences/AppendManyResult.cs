// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Events.Constraints;
using Cratis.Chronicle.Grains.EventSequences.Concurrency;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the result of an append many operation.
/// </summary>
public class AppendManyResult
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the operation.
    /// </summary>
    public CorrelationId CorrelationId { get; init; } = CorrelationId.NotSet;

    /// <summary>
    /// Gets the sequence numbers of the events that were appended, if successful. In the same sequence as the events were provided.
    /// </summary>
    public IEnumerable<EventSequenceNumber> SequenceNumbers { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => !HasConstraintViolations && !HasErrors;

    /// <summary>
    /// Gets whether or not there are any violations that occurred.
    /// </summary>
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <summary>
    /// Gets whether or not there are any errors that occurred.
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Gets any violations that occurred during the operation.
    /// </summary>
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Gets any concurrency violations <see cref="ConcurrencyViolation"/>.
    /// </summary>
    public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; init; } = [];

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
    /// <param name="violations">Constraint violations.</param>
    /// <returns>A new failed <see cref="AppendResult"/> instance.</returns>
    public static AppendManyResult Failed(CorrelationId correlationId, IEnumerable<ConstraintViolation> violations) => new()
    {
        CorrelationId = correlationId,
        ConstraintViolations = violations.ToImmutableList()
    };

    /// <summary>
    /// Create a failed result with constraint violations.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="violations">Concurrency violations.</param>
    /// <returns>A new failed <see cref="AppendResult"/> instance.</returns>
    public static AppendManyResult Failed(CorrelationId correlationId, IEnumerable<ConcurrencyViolation> violations) => new()
    {
        CorrelationId = correlationId,
        ConcurrencyViolations = violations
    };
}

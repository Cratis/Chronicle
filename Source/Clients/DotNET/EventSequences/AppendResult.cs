// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the result of an append operation.
/// </summary>
public record AppendResult : IAppendResult
{
    /// <inheritdoc />
    public CorrelationId CorrelationId { get; init; } = CorrelationId.NotSet;

    /// <summary>
    /// Gets the sequence number of the event that was appended, if successful.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; init; } = EventSequenceNumber.Unavailable;

    /// <inheritdoc />
    public bool IsSuccess => !HasConstraintViolations && !HasErrors && !HasConcurrencyViolations;

    /// <inheritdoc />
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <inheritdoc />
    public bool HasConcurrencyViolations => ConcurrencyViolation is not null;

    /// <inheritdoc />
    public bool HasErrors => Errors.Any();

    /// <inheritdoc />
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets any concurrency violation that occurred during the operation.
    /// </summary>
    public ConcurrencyViolation? ConcurrencyViolation { get; init; }

    /// <inheritdoc />
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Create a successful result.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to report.</param>
    /// <returns>A new successful <see cref="AppendResult"/> instance.</returns>
    public static AppendResult Success(CorrelationId correlationId, EventSequenceNumber sequenceNumber) => new()
    {
        CorrelationId = correlationId,
        SequenceNumber = sequenceNumber
    };

    /// <summary>
    /// Create a failed result with constraint violations.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="violations">The violations.</param>
    /// <returns>A new failed <see cref="AppendResult"/> instance.</returns>
    public static AppendResult Failed(CorrelationId correlationId, IEnumerable<ConstraintViolation> violations) => new()
    {
        CorrelationId = correlationId,
        ConstraintViolations = violations.ToArray()
    };

    /// <summary>
    /// Create a failed result with errors.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> for the operation.</param>
    /// <param name="errors">The errors.</param>
    /// <returns>A new failed <see cref="AppendResult"/> instance.</returns>
    public static AppendResult Failed(CorrelationId correlationId, IEnumerable<AppendError> errors) => new()
    {
        CorrelationId = correlationId,
        Errors = errors.ToArray()
    };
}

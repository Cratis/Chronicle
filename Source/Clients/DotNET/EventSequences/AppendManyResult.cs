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
public record AppendManyResult
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
    public bool IsSuccess => !HasConstraintViolations && !HasErrors && !HasConcurrencyViolations;

    /// <summary>
    /// Gets whether or not there are any violations that occurred.
    /// </summary>
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <summary>
    /// Gets whether or not there are any concurrency violations that occurred.
    /// </summary>
    public bool HasConcurrencyViolations => ConcurrencyViolations.Any();

    /// <summary>
    /// Gets whether or not there are any errors that occurred.
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Gets any violations that occurred during the operation.
    /// </summary>
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets any concurrency violations that occurred during the operation.
    /// </summary>
    public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; init; } = [];

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
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
    /// Merges another <see cref="AppendManyResult"/> with this one, creating a new merged result.
    /// </summary>
    /// <param name="other">The other <see cref="AppendManyResult"/> to merge.</param>
    /// <returns>A new <see cref="AppendManyResult"/> that combines the results of both.</returns>
    public AppendManyResult MergeWith(AppendManyResult other) =>
        new()
        {
            CorrelationId = other.CorrelationId,
            SequenceNumbers = SequenceNumbers.Concat(other.SequenceNumbers).ToImmutableList(),
            ConstraintViolations = ConstraintViolations.Concat(other.ConstraintViolations).ToImmutableList(),
            ConcurrencyViolations = ConcurrencyViolations.Concat(other.ConcurrencyViolations).ToImmutableList(),
            Errors = Errors.Concat(other.Errors).ToImmutableList()
        };
}

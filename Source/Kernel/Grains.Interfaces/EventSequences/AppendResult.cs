// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the result of an append operation.
/// </summary>
public class AppendResult
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the operation.
    /// </summary>
    public CorrelationId CorrelationId { get; init; } = CorrelationId.New();

    /// <summary>
    /// Gets the sequence number of the event that was appended, if successful.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; init; } = EventSequenceNumber.Unavailable;

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
    public IImmutableList<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IImmutableList<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Create a successful result.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to report.</param>
    /// <returns>A new <see cref="AppendResult"/> instance.</returns>
    public static AppendResult Success(EventSequenceNumber sequenceNumber) => new() { SequenceNumber = sequenceNumber };
}

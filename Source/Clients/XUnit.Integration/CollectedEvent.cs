// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a single event append operation that was captured by an <see cref="IEventAppendCollection"/>.
/// </summary>
/// <remarks>
/// The <see cref="Event"/> object is always populated — it is the object that was passed to Append,
/// regardless of whether the operation succeeded. <see cref="SequenceNumber"/> is
/// <see cref="EventSequenceNumber.Unavailable"/> when the append failed.
/// </remarks>
public record CollectedEvent
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the operation.
    /// </summary>
    public required CorrelationId CorrelationId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSourceId"/> the event was appended for.
    /// </summary>
    public required EventSourceId EventSourceId { get; init; }

    /// <summary>
    /// Gets the assigned <see cref="EventSequenceNumber"/>. <see cref="EventSequenceNumber.Unavailable"/> when the append failed.
    /// </summary>
    public required EventSequenceNumber SequenceNumber { get; init; }

    /// <summary>
    /// Gets the event object that was appended (or attempted).
    /// </summary>
    public required object Event { get; init; }

    /// <summary>
    /// Gets the causation chain that was active at the time of the append.
    /// </summary>
    public IImmutableList<Causation> CausationChain { get; init; } = ImmutableList<Causation>.Empty;

    /// <summary>
    /// Gets a value indicating whether the append was successful.
    /// </summary>
    public bool IsSuccess => SequenceNumber.IsActualValue && !HasConstraintViolations && !HasConcurrencyViolations && !HasErrors;

    /// <summary>
    /// Gets whether there are any constraint violations.
    /// </summary>
    public bool HasConstraintViolations => ConstraintViolations.Any();

    /// <summary>
    /// Gets whether there are any concurrency violations.
    /// </summary>
    public bool HasConcurrencyViolations => ConcurrencyViolations.Any();

    /// <summary>
    /// Gets whether there are any errors.
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Gets any constraint violations from the append operation.
    /// </summary>
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets any concurrency violations from the append operation.
    /// </summary>
    public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; init; } = [];

    /// <summary>
    /// Gets any errors from the append operation.
    /// </summary>
    public IEnumerable<AppendError> Errors { get; init; } = [];
}

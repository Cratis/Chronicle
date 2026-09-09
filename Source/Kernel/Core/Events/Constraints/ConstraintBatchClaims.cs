// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Tracks unique constraint values claimed by events within a single batch append.
/// </summary>
/// <remarks>
/// When appending many events as a single operation, every event is validated against the persisted
/// constraint index before any of them are written. Without tracking the values claimed by earlier
/// events in the same batch, two events in the batch could both claim the same unique value and both
/// be appended. This accumulator makes earlier claims visible to later events within the batch so that
/// intra-batch duplicates are rejected the same way cross-batch duplicates are.
/// </remarks>
public sealed class ConstraintBatchClaims
{
    readonly Dictionary<ClaimKey, EventSourceId> _claims = [];
    readonly Dictionary<EventTypeCycleKey, EventTypeCycleState> _eventTypeCycles = [];

    enum EventTypeCycleState
    {
        Open = 0,
        Released = 1
    }

    /// <summary>
    /// Attempt to claim a unique value for an event source within the batch.
    /// </summary>
    /// <param name="constraintName">The <see cref="ConstraintName"/> the value belongs to.</param>
    /// <param name="scopeKey">The scope key the value is constrained within.</param>
    /// <param name="value">The <see cref="UniqueConstraintValue"/> being claimed.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> claiming the value.</param>
    /// <returns>True if the claim is allowed, false if the value is already claimed by a different event source in the batch.</returns>
    public bool TryClaim(ConstraintName constraintName, string scopeKey, UniqueConstraintValue value, EventSourceId eventSourceId)
    {
        var key = new ClaimKey(constraintName, scopeKey, value);
        if (_claims.TryGetValue(key, out var claimant))
        {
            return claimant == eventSourceId;
        }

        _claims[key] = eventSourceId;
        return true;
    }

    /// <summary>
    /// Attempt to claim a unique event type constraint cycle for an event source within the batch.
    /// </summary>
    /// <param name="constraintName">The <see cref="ConstraintName"/> the cycle belongs to.</param>
    /// <param name="scope">The <see cref="ResolvedConstraintScope"/> the cycle is constrained within.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> claiming the cycle.</param>
    /// <param name="durablyAllowed">
    /// Whether the durable, pre-batch history already allows the claim - consulted only the first time this
    /// event source is seen for the constraint within the batch.
    /// </param>
    /// <returns>True if the claim is allowed, false if a covered event earlier in the batch already holds it.</returns>
    /// <remarks>
    /// A batch is always appended strictly after every durably persisted event, so a release recorded earlier
    /// in this same batch (<see cref="ReleaseEventTypeCycle"/>) always postdates the durable history and takes
    /// precedence over it. Durable history is consulted only when the batch itself has not yet said anything
    /// about this event source, scope and constraint.
    /// <para>
    /// Internal: the event type cycle side of the batch state is a collaboration detail between this accumulator
    /// and <see cref="UniqueEventTypeConstraintValidator"/>, not a documented extension point.
    /// </para>
    /// </remarks>
    internal bool TryClaimEventTypeCycle(ConstraintName constraintName, ResolvedConstraintScope? scope, EventSourceId eventSourceId, bool durablyAllowed)
    {
        var key = new EventTypeCycleKey(constraintName, scope, eventSourceId);
        if (_eventTypeCycles.TryGetValue(key, out var state))
        {
            if (state == EventTypeCycleState.Open)
            {
                return false;
            }

            _eventTypeCycles[key] = EventTypeCycleState.Open;
            return true;
        }

        if (!durablyAllowed)
        {
            return false;
        }

        _eventTypeCycles[key] = EventTypeCycleState.Open;
        return true;
    }

    /// <summary>
    /// Record that an event earlier in the batch released a unique event type constraint cycle for an event source.
    /// </summary>
    /// <param name="constraintName">The <see cref="ConstraintName"/> the cycle belongs to.</param>
    /// <param name="scope">The <see cref="ResolvedConstraintScope"/> the cycle is constrained within.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the cycle is released for.</param>
    internal void ReleaseEventTypeCycle(ConstraintName constraintName, ResolvedConstraintScope? scope, EventSourceId eventSourceId) =>
        _eventTypeCycles[new EventTypeCycleKey(constraintName, scope, eventSourceId)] = EventTypeCycleState.Released;

    record ClaimKey(ConstraintName ConstraintName, string ScopeKey, UniqueConstraintValue Value);

    /// <summary>
    /// Identifies one cycle within the batch.
    /// </summary>
    /// <param name="ConstraintName">The <see cref="ConstraintName"/> the cycle belongs to.</param>
    /// <param name="Scope">The <see cref="ResolvedConstraintScope"/> the cycle is constrained within, or <see langword="null"/> when unscoped.</param>
    /// <param name="EventSourceId">The <see cref="EventSourceId"/> the cycle belongs to.</param>
    /// <remarks>
    /// The scope is kept as its typed dimensions rather than a flattened key so that two genuinely different
    /// scopes can never hash to the same cycle - a dimension value containing whatever character a flat key
    /// joins on would otherwise alias onto a different scope's cycle.
    /// </remarks>
    record EventTypeCycleKey(ConstraintName ConstraintName, ResolvedConstraintScope? Scope, EventSourceId EventSourceId);
}

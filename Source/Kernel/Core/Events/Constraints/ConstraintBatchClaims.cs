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

    record ClaimKey(ConstraintName ConstraintName, string ScopeKey, UniqueConstraintValue Value);
}

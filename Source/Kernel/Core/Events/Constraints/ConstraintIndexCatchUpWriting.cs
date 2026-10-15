// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Supplies the grain's live-key guard and durable write-refusal barrier to the shared fold.
/// </summary>
/// <param name="excluded">The live-key exclusion predicate.</param>
/// <param name="pending">The steady-state pending predicate.</param>
/// <param name="refuse">The persisted refusal barrier.</param>
/// <param name="allow">The persisted refusal release.</param>
internal sealed class ConstraintIndexCatchUpWriting(
    Func<ConstraintIndexReplayDifference, bool> excluded,
    Func<ConstraintName, bool> pending,
    Func<IEnumerable<ConstraintName>, CancellationToken, Task> refuse,
    Func<CancellationToken, Task> allow)
{
    /// <summary>
    /// Gets or sets whether the entire write phase needs refusal.
    /// </summary>
    internal bool UnderRefusal { get; set; }

    /// <summary>
    /// Tests whether a difference has been superseded live.
    /// </summary>
    /// <param name="difference">The staged key.</param>
    /// <returns>Whether it must be skipped.</returns>
    internal bool IsExcluded(ConstraintIndexReplayDifference difference) => excluded(difference);

    /// <summary>
    /// Tests whether steady-state repair must finish first.
    /// </summary>
    /// <param name="name">The constraint.</param>
    /// <returns>Whether it is pending.</returns>
    internal bool IsPending(ConstraintName name) => pending(name);

    /// <summary>
    /// Persists refusal before a write turn begins.
    /// </summary>
    /// <param name="names">The constraints to refuse.</param>
    /// <param name="token">The deadline.</param>
    /// <returns>The persistence task.</returns>
    internal Task Refuse(IEnumerable<ConstraintName> names, CancellationToken token) => refuse(names, token);

    /// <summary>
    /// Clears the marker after every group started by the turn is complete.
    /// </summary>
    /// <param name="token">The deadline.</param>
    /// <returns>The persistence task.</returns>
    internal Task Allow(CancellationToken token) => allow(token);
}

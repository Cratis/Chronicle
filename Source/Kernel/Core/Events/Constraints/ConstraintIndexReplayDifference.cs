// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Holds one final difference and its mutable confirmed-write progress.
/// </summary>
/// <param name="name">The constraint.</param>
/// <param name="position">The deciding event.</param>
/// <param name="release">The old value release.</param>
/// <param name="claim">The final claim or closure transition.</param>
/// <param name="scopeKey">The unique scope.</param>
/// <param name="value">The final claimed value.</param>
/// <param name="previousValues">The old values this key releases.</param>
internal sealed class ConstraintIndexReplayDifference(ConstraintName name, EventSequenceNumber position, Func<CancellationToken, Task>? release, Func<CancellationToken, Task>? claim, string scopeKey, UniqueConstraintValue? value, IReadOnlyList<UniqueConstraintValue> previousValues)
{
    /// <summary>
    /// Gets the constraint.
    /// </summary>
    internal ConstraintName Name { get; } = name;

    /// <summary>
    /// Gets the deciding event.
    /// </summary>
    internal EventSequenceNumber Position { get; } = position;

    /// <summary>
    /// Gets the old value release.
    /// </summary>
    internal Func<CancellationToken, Task>? Release { get; } = release;

    /// <summary>
    /// Gets the final claim.
    /// </summary>
    internal Func<CancellationToken, Task>? Claim { get; } = claim;

    /// <summary>
    /// Gets the unique scope.
    /// </summary>
    internal string ScopeKey { get; } = scopeKey;

    /// <summary>
    /// Gets the final claimed value.
    /// </summary>
    internal UniqueConstraintValue? Value { get; } = value;

    /// <summary>
    /// Gets the old values this key releases.
    /// </summary>
    internal IReadOnlyList<UniqueConstraintValue> PreviousValues { get; } = previousValues;

    /// <summary>
    /// Gets or sets whether the release has been confirmed.
    /// </summary>
    internal bool Released { get; set; }

    /// <summary>
    /// Gets or sets whether the claim has been confirmed or settled.
    /// </summary>
    internal bool Claimed { get; set; }

    /// <summary>
    /// Gets or sets whether a changed holder requires one claim retry.
    /// </summary>
    internal bool Retry { get; set; }
}

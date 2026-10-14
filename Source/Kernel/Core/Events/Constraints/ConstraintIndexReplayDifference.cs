// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Holds one final difference and its confirmed write progress.
/// </summary>
/// <param name="Name">The constraint.</param>
/// <param name="Position">The deciding event.</param>
/// <param name="Release">The old value release.</param>
/// <param name="Claim">The final claim or closure transition.</param>
/// <param name="ScopeKey">The unique scope.</param>
/// <param name="Value">The final claimed value.</param>
/// <param name="PreviousValues">The old values this key releases.</param>
internal sealed record ConstraintIndexReplayDifference(
    ConstraintName Name,
    EventSequenceNumber Position,
    Func<CancellationToken, Task>? Release,
    Func<CancellationToken, Task>? Claim,
    string ScopeKey,
    UniqueConstraintValue? Value,
    IReadOnlyList<UniqueConstraintValue> PreviousValues)
{
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims;

/// <summary>
/// Identifies a diagnostic disagreement between shadow claims and authoritative derived sets.
/// </summary>
internal enum ShadowClaimDivergence
{
    /// <summary>
    /// A claim has no corresponding derived-set membership.
    /// </summary>
    ClaimWithoutDerivedSet = 0,

    /// <summary>
    /// Derived-set membership has no matching claim or observer-wide coverage.
    /// </summary>
    DerivedSetWithoutClaim = 1,

    /// <summary>
    /// A partition claim disagrees with the kind of its derived-set membership.
    /// </summary>
    KindMismatch = 2,

    /// <summary>
    /// A partition left its derived set without an explicit hand-over.
    /// </summary>
    ReleasedWithoutHandOver = 3,

    /// <summary>
    /// Partition and observer-wide claims are held concurrently.
    /// </summary>
    ExclusionViolated = 4
}

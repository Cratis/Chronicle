// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Identifies the work holding a shadow ownership claim.
/// </summary>
public enum ClaimHolderKind
{
    /// <summary>
    /// No work holds the claim.
    /// </summary>
    None = 0,

    /// <summary>
    /// A partition replay holds the claim.
    /// </summary>
    PartitionReplay = 1,

    /// <summary>
    /// A partition catch-up holds the claim.
    /// </summary>
    PartitionCatchUp = 2,

    /// <summary>
    /// Failure recovery holds the claim.
    /// </summary>
    FailureRecovery = 3,

    /// <summary>
    /// An observer-wide catch-up holds the claim.
    /// </summary>
    ObserverCatchUp = 4,

    /// <summary>
    /// An observer-wide replay holds the claim.
    /// </summary>
    ObserverReplay = 5
}

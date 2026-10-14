// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Names the resumable stages of one fixed-range fold.
/// </summary>
internal enum ConstraintIndexReplayPhase
{
    /// <summary>
    /// Fold the current key segment.
    /// </summary>
    Reading = 0,

    /// <summary>
    /// Compare staged keys against storage.
    /// </summary>
    Collecting = 1,

    /// <summary>
    /// Release differences before claims.
    /// </summary>
    Releasing = 2,

    /// <summary>
    /// Write claims in deciding order.
    /// </summary>
    Claiming = 3,

    /// <summary>
    /// Every difference has been written or settled.
    /// </summary>
    Complete = 4
}

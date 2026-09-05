// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Cuts;

/// <summary>
/// Defines the outcome of capturing one read model's payload at an exact cut.
/// </summary>
public enum ReadModelCutOutcome
{
    /// <summary>
    /// The outcome is unknown. Never a produced value - a closed sentinel guarding against an unset field.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The payload was recomputed and its digest verified.
    /// </summary>
    Captured = 1,

    /// <summary>
    /// The read model is not backed by a projection - reducer-backed read models run in the connected client
    /// process, not the kernel, so the kernel cannot recompute them without a live client round trip.
    /// </summary>
    Unsupported = 2,

    /// <summary>
    /// A mutation operation could still touch content at or below the requested cut, so the content is not
    /// yet stable enough to capture exactly.
    /// </summary>
    MutationInProgress = 3,

    /// <summary>
    /// The read model identifier named in the selection does not exist.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Recomputing the payload failed for a reason other than the above.
    /// </summary>
    Failed = 5,
}

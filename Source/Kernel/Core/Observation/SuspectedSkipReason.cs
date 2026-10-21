// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes why an observer's position is suspected to have moved past a live event without delivering it.
/// </summary>
internal enum SuspectedSkipReason
{
    /// <summary>
    /// The event was held back while a catch-up round was being prepared.
    /// </summary>
    HeldBackWhilePreparingCatchUp = 0,

    /// <summary>
    /// The event was held back while its partition was replaying.
    /// </summary>
    HeldBackWhileReplaying = 1,

    /// <summary>
    /// The event was held back while its partition was catching up.
    /// </summary>
    HeldBackWhileCatchingUp = 2,

    /// <summary>
    /// The event was discarded as already handled after another partition's job moved the position past it.
    /// </summary>
    PassedByPartitionJob = 3
}

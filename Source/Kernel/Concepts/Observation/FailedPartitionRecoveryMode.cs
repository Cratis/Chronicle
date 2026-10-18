// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The delivery mode of a failed partition's recovery cursor.
/// </summary>
public enum FailedPartitionRecoveryMode
{
    /// <summary>
    /// Ordinary live delivery, including recovery of legacy failed entries.
    /// </summary>
    Live = 0,

    /// <summary>
    /// Replay delivery through the cursor's replay boundary.
    /// </summary>
    Replay = 1
}

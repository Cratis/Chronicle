// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the replay state for observer notifications.
/// </summary>
[ProtoContract]
public enum ReplayState
{
    /// <summary>
    /// No replay state - regular event processing.
    /// </summary>
    None = 0,

    /// <summary>
    /// A replay is beginning.
    /// </summary>
    BeginReplay = 1,

    /// <summary>
    /// A replay has ended.
    /// </summary>
    EndReplay = 2,

    /// <summary>
    /// Replay of a specific partition is beginning.
    /// </summary>
    BeginReplayPartition = 3,

    /// <summary>
    /// Replay of a specific partition has ended.
    /// </summary>
    EndReplayPartition = 4,
}

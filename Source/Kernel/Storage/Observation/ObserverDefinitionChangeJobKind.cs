// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The reader obligation an inventory job executes.
/// </summary>
public enum ObserverDefinitionChangeJobKind
{
    /// <summary>
    /// Observer-wide catch-up.
    /// </summary>
    CatchUp = 0,

    /// <summary>
    /// Catch-up of one partition.
    /// </summary>
    PartitionCatchUp = 1,

    /// <summary>
    /// Failed-partition retry.
    /// </summary>
    Retry = 2,

    /// <summary>
    /// Resumption of a handler hold.
    /// </summary>
    HoldResume = 3,

    /// <summary>
    /// Full or partition replay.
    /// </summary>
    Replay = 4
}

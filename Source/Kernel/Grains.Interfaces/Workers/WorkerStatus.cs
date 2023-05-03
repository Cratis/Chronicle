// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// The status of a worker.
/// </summary>
public enum WorkerStatus
{
    /// <summary>
    /// Unknown state.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Worker is idle.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Worker is working.
    /// </summary>
    Working = 1,

    /// <summary>
    /// Worker is completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Worker failed.
    /// </summary>
    Failed = 3
}

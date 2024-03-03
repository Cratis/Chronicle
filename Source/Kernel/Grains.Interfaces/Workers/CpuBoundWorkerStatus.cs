﻿// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Grains.Workers;

/// <summary>
/// The status of the <see cref="ICpuBoundWorker{TRequest, TResult}"/> grain.
/// </summary>
public enum CpuBoundWorkerStatus
{
    /// <summary>
    /// Work has not yet been started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Worker is actively running or is in queue to be run.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The worker has been completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The worker has been completed with failure.
    /// </summary>
    Failed = 3
}

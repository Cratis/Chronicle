// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the status of a job step.
/// </summary>
public enum JobStepStatus
{
    /// <summary>
    /// Represents an unset status.
    /// </summary>
    None = 0,

    /// <summary>
    /// The job step has not started.
    /// </summary>
    NotStarted = 1,

    /// <summary>
    /// The job step is running.
    /// </summary>
    Running = 2,

    /// <summary>
    /// The job step is completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The job step failed.
    /// </summary>
    Failed = 4
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Represents the status of a job step.
/// </summary>
public enum JobStepStatus
{
    /// <summary>
    /// Represents an unset status.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The job step has been scheduled to start.
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// The job step is running.
    /// </summary>
    Running = 2,

    /// <summary>
    /// The job step is completed.
    /// </summary>
    Succeeded = 3,

    /// <summary>
    /// The job step failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The job step has been stopped.
    /// </summary>
    Paused = 5,

    /// <summary>
    /// The job step has been stopped.
    /// </summary>
    Stopped = 6
}

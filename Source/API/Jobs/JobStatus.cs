// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Jobs;

/// <summary>
/// Represents the status of a job.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Represents an unset status.
    /// </summary>
    None = 0,

    /// <summary>
    /// The job has been started and is running.
    /// </summary>
    Preparing = 1,

    /// <summary>
    /// The job has been started and is running.
    /// </summary>
    PreparingSteps = 2,

    /// <summary>
    /// The job has been started and is running.
    /// </summary>
    PreparingStepsForRunning = 3,

    /// <summary>
    /// The job has been started and is running.
    /// </summary>
    StartingSteps = 4,

    /// <summary>
    /// The job has been started and is running.
    /// </summary>
    Running = 5,

    /// <summary>
    /// The job has been completed successfully.
    /// </summary>
    CompletedSuccessfully = 6,

    /// <summary>
    /// The job has completed with failures.
    /// </summary>
    CompletedWithFailures = 7,

    /// <summary>
    /// The job has been paused.
    /// </summary>
    Paused = 8,

    /// <summary>
    /// The job has been cancelled.
    /// </summary>
    Stopped = 9,

    /// <summary>
    /// The job has failed and can't recover.
    /// </summary>
    Failed = 10
}

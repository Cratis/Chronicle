// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

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
    /// The job has been started and is preparing itself.
    /// </summary>
    PreparingJob = 1,

    /// <summary>
    /// The job has been started and is running.
    /// </summary>
    Running = 2,

    /// <summary>
    /// The job has been completed successfully.
    /// </summary>
    CompletedSuccessfully = 3,

    /// <summary>
    /// The job has completed with failures.
    /// </summary>
    CompletedWithFailures = 4,

    /// <summary>
    /// The job has been stopped and can be resumed later.
    /// </summary>
    Stopped = 5,

    /// <summary>
    /// The job has failed and can't recover.
    /// </summary>
    Failed = 6,

    /// <summary>
    /// The job is scheduled to be removed.
    /// </summary>
    Removing = 7
}

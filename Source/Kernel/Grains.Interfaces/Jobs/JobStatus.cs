// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the status of a <see cref="IJob{TRequest}"/>.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Represents an unset status.
    /// </summary>
    None = 0,

    /// <summary>
    /// The job has been started.
    /// </summary>
    Started = 1,

    /// <summary>
    /// The job has been completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The job has completed with failures.
    /// </summary>
    CompletedWithFailures = 3,

    /// <summary>
    /// The job has been paused.
    /// </summary>
    Paused = 4,

    /// <summary>
    /// The job has been cancelled.
    /// </summary>
    Stopped = 5
}

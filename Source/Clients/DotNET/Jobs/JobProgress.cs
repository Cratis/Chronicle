// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
public class JobProgress
{
    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps { get; init; }

    /// <summary>
    /// Gets or sets the completed number of steps.
    /// </summary>
    public int SuccessfulSteps { get; init; }

    /// <summary>
    /// Gets or sets the failed number of steps.
    /// </summary>
    public int FailedSteps { get; init; }

    /// <summary>
    /// Gets or sets the number of stopped steps.
    /// </summary>
    public int StoppedSteps { get; set; }

    /// <summary>
    /// Gets whether or not the job is completed.
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Gets whether the job is completed where it also can have stopped steps.
    /// </summary>
    public bool IsStopped { get; init; }

    /// <summary>
    /// Gets or sets the current message associated with the progress.
    /// </summary>
    public JobProgressMessage Message { get; init; } = JobProgressMessage.None;
}

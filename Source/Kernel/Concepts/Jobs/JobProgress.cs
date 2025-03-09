// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
public class JobProgress
{
    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the completed number of steps.
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// Gets or sets the failed number of steps.
    /// </summary>
    public int FailedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of stopped steps.
    /// </summary>
    public int StoppedSteps { get; set; }

    /// <summary>
    /// Gets whether the job is completed and there are no stopped steps.
    /// </summary>
    public bool IsCompleted => SuccessfulSteps + FailedSteps == TotalSteps;

    /// <summary>
    /// Gets whether the job is completed where it also can have stopped steps.
    /// </summary>
    public bool IsStopped => SuccessfulSteps + FailedSteps + StoppedSteps == TotalSteps;

    /// <summary>
    /// Gets or sets the current <see cref="JobProgressMessage"/> associated with the progress.
    /// </summary>
    public JobProgressMessage Message { get; set; } = JobProgressMessage.None;
}

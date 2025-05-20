// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
/// <param name="TotalSteps">The total number of steps.</param>
/// <param name="SuccessfulSteps">The completed number of steps.</param>
/// <param name="FailedSteps">The failed number of steps.</param>
/// <param name="StoppedSteps">The number of stopped steps.</param>
/// <param name="IsCompleted">Whether or not the job is completed.</param>
/// <param name="IsStopped">Whether or not the job is stopped.</param>
/// <param name="Message">The current message associated with the progress.</param>
public record JobProgress(
    int TotalSteps,
    int SuccessfulSteps,
    int FailedSteps,
    int StoppedSteps,
    bool IsCompleted,
    bool IsStopped,
    string Message);

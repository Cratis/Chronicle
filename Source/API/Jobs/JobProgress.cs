// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
/// <param name="TotalSteps">Total steps of the job.</param>
/// <param name="SuccessfulSteps">Number of successful steps.</param>
/// <param name="FailedSteps">Number of failed steps.</param>
/// <param name="IsCompleted">Whether or not the job has completed.</param>
/// <param name="Message">Current message from the job.</param>
public record JobProgress(int TotalSteps, int SuccessfulSteps, int FailedSteps, bool IsCompleted, string Message);

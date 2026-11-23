// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Defines the counting of jobs that failed, for the metrics of the kernel.
/// </summary>
public interface IJobFailureMetrics
{
    /// <summary>
    /// Counts a job that ended with failed steps, or could not prepare its steps.
    /// </summary>
    /// <param name="jobType">The type of the job, in lowercase with underscores.</param>
    void Failed(string jobType);
}

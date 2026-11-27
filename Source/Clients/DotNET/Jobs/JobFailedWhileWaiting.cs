// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// A terminal job failure was observed while waiting for jobs to drain successfully.
/// </summary>
/// <param name="job">The failed job from the poll that ended the wait.</param>
public sealed class JobFailedWhileWaiting(Job job) : Exception($"Job {job.Id.Value:D} reached terminal status {job.Status}.")
{
    /// <summary>
    /// Gets the failed job from the poll that ended the wait.
    /// </summary>
    public Job Job { get; } = job;
}

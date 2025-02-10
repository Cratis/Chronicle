// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// Holds the state of a job.
/// </summary>
public class JobState
{
    /// <summary>
    /// Gets or sets the <see cref="JobId"/>.
    /// </summary>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <summary>
    /// Gets or sets the details for a job.
    /// </summary>
    public JobDetails Details { get; set; } = JobDetails.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobType"/>.
    /// </summary>
    public JobType Type { get; set; } = JobType.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobStatus"/>.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the time that this job was created.
    /// </summary>
    public DateTimeOffset Created { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    public IList<JobStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    public JobProgress Progress { get; set; } = new();

    /// <summary>
    /// Gets or sets the request associated with the job.
    /// </summary>
    public IJobRequest Request { get; set; } = default!;

    /// <summary>
    /// Gets whether the job is resumable.
    /// </summary>
    public bool IsResumable =>
        Status == JobStatus.None ||
        Status == JobStatus.Paused ||
        Status == JobStatus.Stopped;
}

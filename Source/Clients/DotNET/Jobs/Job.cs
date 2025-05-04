// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the state of a job.
/// </summary>
public class Job
{
    /// <summary>
    /// Gets or sets the unique identifier for the job.
    /// </summary>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <summary>
    /// Gets or sets the details for a job.
    /// </summary>
    public JobDetails Details { get; set; } = JobDetails.NotSet;

    /// <summary>
    /// Gets or sets the type of the job.
    /// </summary>
    public JobType Type { get; set; } = JobType.NotSet;

    /// <summary>
    /// Gets or sets the status of the job.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when job was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    public IList<JobStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    public JobProgress Progress { get; set; } = new();
}

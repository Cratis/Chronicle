// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Holds the state of a <see cref="IJob{TRequest}"/>.
/// </summary>
public class JobState
{
    /// <summary>
    /// Gets or sets the <see cref="JobId"/>.
    /// </summary>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    public JobName Name { get; set; } = JobName.NotSet;

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
    public JobStatus Status => StatusChanges.Count == 0 ? JobStatus.None : StatusChanges.Last().Status;

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    public IList<JobStatusChanged> StatusChanges { get; set; } = new List<JobStatusChanged>();

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    public JobProgress Progress { get; set; } = new();

    /// <summary>
    /// Gets or sets the request associated with the job.
    /// </summary>
    public object Request { get; set; } = default!;
}

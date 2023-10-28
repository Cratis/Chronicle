// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Defines the common content of state for a job.
/// </summary>
public interface IJobState
{
    /// <summary>
    /// Gets or sets the <see cref="JobId"/>.
    /// </summary>
    JobId Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    JobName Name { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JobType"/>.
    /// </summary>
    JobType Type { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JobStatus"/>.
    /// </summary>
    JobStatus Status { get; }

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    IList<JobStatusChanged> StatusChanges { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    JobProgress Progress { get; set; }

    /// <summary>
    /// Gets whether or not the job should be removed.
    /// </summary>
    bool Remove { get; set; }
}

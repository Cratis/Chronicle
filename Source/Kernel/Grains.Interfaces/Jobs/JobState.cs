// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Holds the state of a <see cref="IJob{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
public class JobState<TRequest> : IJobState
{
    /// <inheritdoc/>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <inheritdoc/>
    public JobName Name { get; set; } = JobName.NotSet;

    /// <inheritdoc/>
    public JobType Type { get; set; } = JobType.NotSet;

    /// <inheritdoc/>
    public JobStatus Status => StatusChanges.Count == 0 ? JobStatus.None : StatusChanges.Last().Status;

    /// <inheritdoc/>
    public IList<JobStatusChanged> StatusChanges { get; set; } = new List<JobStatusChanged>();

    /// <inheritdoc/>
    public JobProgress Progress { get; set; } = new();

    /// <inheritdoc/>
    public bool Remove { get; set; }

    /// <summary>
    /// Gets or sets the request associated with the job.
    /// </summary>
    public TRequest Request { get; set; } = default!;
}

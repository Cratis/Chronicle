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
    public JobId Id { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the <see cref="JobStatus"/>.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets collection of <see cref="JobEvent"/>.
    /// </summary>
    public IList<JobEvent> Events { get; set; } = new List<JobEvent>();

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    public JobProgress Progress { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of <see cref="JobStepState"/>.
    /// </summary>
    public IDictionary<JobStepId, JobStepState> Steps { get; set; } = new Dictionary<JobStepId, JobStepState>();

    /// <summary>
    /// Gets or sets the collection of <see cref="JobStepState"/> for failed job steps.
    /// </summary>
    public IDictionary<JobStepId, JobStepState> FailedSteps { get; set; } = new Dictionary<JobStepId, JobStepState>();
}

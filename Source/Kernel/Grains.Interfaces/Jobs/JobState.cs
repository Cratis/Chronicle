// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Holds the state of a <see cref="IJob"/>.
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
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalStepsCount { get; set; }

    /// <summary>
    /// Gets or sets the number of completed steps.
    /// </summary>
    public int CompletedStepsCount { get; set; }

    /// <summary>
    /// Gets or set the number of failed steps.
    /// </summary>
    public int FailedStepCount { get; set; }
}

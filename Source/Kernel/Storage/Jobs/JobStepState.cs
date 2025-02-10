// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// Represents the state of a job step.
/// </summary>
public class JobStepState
{
    /// <summary>
    /// Gets or sets the <see cref="JobStepId"/>.
    /// </summary>
    public JobStepIdentifier Id { get; set; } = JobStepIdentifier.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobStepType"/> .
    /// </summary>
    public JobStepType Type { get; set; } = JobStepType.NotSet;

    /// <summary>
    /// Gets or sets the name of the job step.
    /// </summary>
    public JobStepName Name { get; set; } = JobStepName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="JobStepStatus"/>.
    /// </summary>
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job step.
    /// </summary>
    public IList<JobStepStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobStepProgress"/>.
    /// </summary>
    public JobStepProgress Progress { get; set; } = new();
}

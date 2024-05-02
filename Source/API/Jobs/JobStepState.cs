// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Jobs;

/// <summary>
/// Represents the current state of a job step.
/// </summary>
/// <param name="Id">The unique identifier of the job.</param>
/// <param name="Type">Type of job.</param>
/// <param name="Name">The name of the job.</param>
/// <param name="Status">The <see cref="JobStatus"/> for the job.</param>
/// <param name="StatusChanges">Any <see cref="JobStatusChanged"/> changes.</param>
/// <param name="Progress">Current progress.</param>
public record JobStepState(Guid Id, string Type, string Name, JobStepStatus Status, IEnumerable<JobStepStatusChanged> StatusChanges, JobStepProgress Progress);

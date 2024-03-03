// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Jobs;

/// <summary>
/// Represents the identifier of a job step.
/// </summary>
/// <param name="JobId">The <see cref="JobId"/> the step belongs to.</param>
/// <param name="JobStepId">The <see cref="JobStepId"/> of the step. </param>
public record JobStepIdentifier(JobId JobId, JobStepId JobStepId)
{
    /// <summary>
    /// Gets the not set identifier.
    /// </summary>
    public static readonly JobStepIdentifier NotSet = new(JobId.NotSet, JobStepId.Unknown);
}

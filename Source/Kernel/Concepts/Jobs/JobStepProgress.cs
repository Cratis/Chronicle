// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Represents the progress of a step.
/// </summary>
public class JobStepProgress
{
    /// <summary>
    /// Gets or sets the percentage of the step.
    /// </summary>
    public JobStepPercentage Percentage { get; set; } = 0;

    /// <summary>
    /// Gets or sets the current <see cref="JobStepProgressMessage"/> associated with the progress.
    /// </summary>
    public JobStepProgressMessage Message { get; set; } = JobStepProgressMessage.None;
}

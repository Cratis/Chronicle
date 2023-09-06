// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
/// <param name="CurrentStep">The current <see cref="JobStepId"/>.</param>
/// <param name="TotalSteps">The total number of <see cref="JobStepId"/>.</param>
/// <param name="Message">The <see cref="JobProgressMessage"/> associated with the progress.</param>
public record JobProgress(JobStepId CurrentStep, JobStepId TotalSteps, JobProgressMessage Message);

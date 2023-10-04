// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the state of a job step.
/// </summary>
public class JobStepState
{
    /// <summary>
    /// Gets or sets the <see cref="JobStepStatus"/>.
    /// </summary>
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JobStepProgress"/>.
    /// </summary>
    public JobStepProgress Progress { get; set; } = new();
}

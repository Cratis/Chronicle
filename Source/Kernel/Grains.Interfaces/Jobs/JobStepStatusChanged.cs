// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a state change event.
/// </summary>
public class JobStepStatusChanged
{
    /// <summary>
    /// Gets or sets the <see cref="JobStepStatus"/>.
    /// </summary>
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; }
}

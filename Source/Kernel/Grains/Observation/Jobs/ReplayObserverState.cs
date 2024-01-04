// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Storage.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="ReplayObserver"/> job step.
/// </summary>
public class ReplayObserverState : JobState
{
    /// <summary>
    /// Gets or sets the number of handled events by the job.
    /// </summary>
    public EventCount HandledCount { get; set; } = EventCount.Zero;
}

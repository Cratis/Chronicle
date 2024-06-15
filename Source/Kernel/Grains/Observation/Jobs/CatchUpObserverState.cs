// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;
using Cratis.Events;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="CatchUpObserver"/> job step.
/// </summary>
public class CatchUpObserverState : JobState
{
    /// <summary>
    /// Gets or sets the number of handled events by the job.
    /// </summary>
    public EventCount HandledCount { get; set; } = EventCount.Zero;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Extension methods for <see cref="EventObservationState"/> used by observer jobs.
/// </summary>
public static class EventObservationStateExtensions
{
    /// <summary>
    /// Gets the <see cref="ObservationOrigin"/> a job reading events with the given <see cref="EventObservationState"/> delivers from.
    /// </summary>
    /// <param name="state">The <see cref="EventObservationState"/> the job sets on the events it reads.</param>
    /// <returns><see cref="ObservationOrigin.Replay"/> for a replay, otherwise <see cref="ObservationOrigin.CatchUp"/>.</returns>
    public static ObservationOrigin ToObservationOrigin(this EventObservationState state) =>
        state.HasFlag(EventObservationState.Replay) ? ObservationOrigin.Replay : ObservationOrigin.CatchUp;
}

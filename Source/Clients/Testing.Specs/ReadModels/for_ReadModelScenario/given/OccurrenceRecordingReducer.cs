// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Captures the occurrence time from the context supplied by the scenario's reducer processor.
/// </summary>
public class OccurrenceRecordingReducer : IReducerFor<RecordedOccurrence>
{
    /// <summary>
    /// Records the delivered occurrence time.
    /// </summary>
    /// <param name="event">The opening event.</param>
    /// <param name="current">The current recorded occurrence, or null before the first event.</param>
    /// <param name="context">The delivered event context.</param>
    /// <returns>The recorded occurrence.</returns>
    public RecordedOccurrence Record(ThingOpened @event, RecordedOccurrence? current, EventContext context) => new(context.Occurred);
}

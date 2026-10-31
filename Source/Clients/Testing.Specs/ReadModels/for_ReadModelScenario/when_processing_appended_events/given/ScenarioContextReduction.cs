// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

/// <summary>
/// Retains every delivered context to verify reducer ordering and source isolation.
/// </summary>
/// <param name="Contexts">The delivered contexts.</param>
public record ScenarioContextReduction(IEnumerable<EventContext> Contexts)
{
    /// <summary>
    /// Records the context delivered to the reducer.
    /// </summary>
    /// <param name="event">The recorded event.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The original context.</param>
    /// <returns>The accumulated contexts.</returns>
    public static ScenarioContextReduction Reduce(ScenarioContextRecorded @event, ScenarioContextReduction? current, EventContext context) =>
        new([.. current?.Contexts ?? [], context]);
}

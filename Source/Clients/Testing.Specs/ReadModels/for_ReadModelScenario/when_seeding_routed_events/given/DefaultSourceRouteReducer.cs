// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Counts only events whose source type is the literal default.
/// </summary>
[EventSourceType("Default")]
public class DefaultSourceRouteReducer : IReducerFor<DefaultSourceEventRoute>
{
    /// <summary>
    /// Counts a delivered event.
    /// </summary>
    /// <param name="event">The recorded event.</param>
    /// <param name="current">The current summary.</param>
    /// <returns>The updated summary.</returns>
    public DefaultSourceEventRoute Record(RoutedEventRecorded @event, DefaultSourceEventRoute? current) =>
        new((current?.Count ?? 0) + 1);
}

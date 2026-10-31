// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Counts only events from order sources, across all stream types.
/// </summary>
[EventSourceType("Order")]
public class SourceFilteredRouteReducer : IReducerFor<SourceFilteredEventRoute>
{
    /// <summary>
    /// Counts a delivered event and captures its position.
    /// </summary>
    /// <param name="event">The recorded event.</param>
    /// <param name="current">The current summary.</param>
    /// <param name="context">The seeded event context.</param>
    /// <returns>The updated summary.</returns>
    public SourceFilteredEventRoute Record(RoutedEventRecorded @event, SourceFilteredEventRoute? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, context.SequenceNumber);
}

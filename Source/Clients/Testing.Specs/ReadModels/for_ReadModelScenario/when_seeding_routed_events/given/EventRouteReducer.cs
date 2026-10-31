// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Captures routing passed to reducer handlers without narrowing the source or stream.
/// </summary>
public class EventRouteReducer : IReducerFor<ReducedEventRoute>
{
    /// <summary>
    /// Captures the event context and increments the count.
    /// </summary>
    /// <param name="event">The recorded event.</param>
    /// <param name="current">The current route summary.</param>
    /// <param name="context">The seeded event context.</param>
    /// <returns>The updated summary.</returns>
    public ReducedEventRoute Record(RoutedEventRecorded @event, ReducedEventRoute? current, EventContext context) =>
        new(context.EventSourceType, context.EventStreamType, context.EventStreamId, (current?.Count ?? 0) + 1, context.SequenceNumber);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces a self-describing event destination that differs from the reactor's metadata.
/// </summary>
[EventSourceType("RouteOrder")]
[EventStreamType("Payments")]
[EventStreamId("reactor-stream")]
public class WrappedRouteReactor : IReactor, ICanProvideEventSourceId
{
    /// <inheritdoc/>
    public EventSourceId GetEventSourceId() => "reactor-destination";

    /// <summary>
    /// Returns an event for an explicit destination.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The self-describing event.</returns>
    public EventForEventSourceId Handle(ReactorRouteRecorded @event) => new("member", new ProducedRouteRecorded(@event.Note))
    {
        EventSourceType = "Member",
        EventStreamType = "Activity",
        EventStreamId = "wrapper-stream"
    };
}

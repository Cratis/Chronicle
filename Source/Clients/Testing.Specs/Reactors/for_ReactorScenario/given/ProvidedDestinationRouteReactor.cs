// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces an event using reactor-level destination metadata.
/// </summary>
[EventSourceType("RouteOrder")]
[EventStreamType("Payments")]
[EventStreamId("produced-stream")]
public class ProvidedDestinationRouteReactor : IReactor, ICanProvideEventSourceId
{
    /// <inheritdoc/>
    public EventSourceId GetEventSourceId() => "destination";

    /// <summary>
    /// Returns an event with reactor-level routing.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The produced event.</returns>
    public ProducedRouteRecorded Handle(ReactorRouteRecorded @event) => new(@event.Note);
}

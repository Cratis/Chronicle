// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations.given;

/// <summary>
/// Produces a bare event using a dynamically provided stream id.
/// </summary>
public class ProvidedStreamIdRouteReactor : IReactor, ICanProvideEventStreamId
{
    /// <inheritdoc/>
    public EventStreamId GetEventStreamId() => "provided-stream";

    /// <summary>
    /// Returns a bare event whose stream id comes from the reactor.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The produced event.</returns>
    public ProducedRouteRecorded Handle(ReactorRouteRecorded @event) => new(@event.Note);
}

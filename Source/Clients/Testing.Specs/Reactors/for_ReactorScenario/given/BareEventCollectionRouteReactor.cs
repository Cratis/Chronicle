// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces a collection of bare events.
/// </summary>
public class BareEventCollectionRouteReactor : IReactor
{
    /// <summary>
    /// Returns two events for the triggering source.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The produced events.</returns>
    public IEnumerable<ProducedRouteRecorded> Handle(ReactorRouteRecorded @event) =>
        [new(@event.Note), new("second")];
}

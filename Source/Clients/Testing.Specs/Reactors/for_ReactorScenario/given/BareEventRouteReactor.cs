// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces an event without destination metadata.
/// </summary>
public class BareEventRouteReactor : IReactor
{
    /// <summary>
    /// Returns an event for the triggering source.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The produced event.</returns>
    public Task<ProducedRouteRecorded> Handle(ReactorRouteRecorded @event) => Task.FromResult(new ProducedRouteRecorded(@event.Note));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces an event through a typed source definition.
/// </summary>
public class TypedProducedRouteReactor : IReactor
{
    /// <summary>
    /// Returns an event with destination types resolved from a definition.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The event with typed destination routing.</returns>
    public EventForEventSourceId Handle(ReactorRouteRecorded @event) => new("order", new ProducedRouteRecorded(@event.Note))
    {
        EventSource = typeof(RoutedOrderSource),
        EventStream = "Payments",
        EventStreamId = "typed-stream"
    };
}

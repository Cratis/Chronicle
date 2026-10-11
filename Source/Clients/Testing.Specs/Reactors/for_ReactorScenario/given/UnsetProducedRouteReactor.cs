// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces an event with unset destination values.
/// </summary>
public class UnsetProducedRouteReactor : IReactor
{
    /// <summary>
    /// Returns an event whose unset values take event-log defaults.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The event with unset destination metadata.</returns>
    public EventForEventSourceId Handle(ReactorRouteRecorded @event) => new("member", new ProducedRouteRecorded(@event.Note))
    {
        EventSourceType = EventSourceType.Unspecified,
        EventStreamType = new EventStreamType(string.Empty),
        EventStreamId = EventStreamId.NotSet
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_recording_produced_destinations.given;

/// <summary>
/// Produces events for two independent destinations.
/// </summary>
public class SeveralEventSourcesRouteReactor : IReactor
{
    /// <summary>
    /// Returns events with distinct source and stream routes.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>The self-describing events.</returns>
    public IEnumerable<EventForEventSourceId> Handle(ReactorRouteRecorded @event) =>
    [
        new("member", new ProducedRouteRecorded(@event.Note))
        {
            EventSourceType = "Member",
            EventStreamType = "Activity",
            EventStreamId = "member-stream"
        },
        new("account", new ProducedRouteRecorded("second"))
        {
            EventSourceType = "Account",
            EventStreamType = "Transactions",
            EventStreamId = "account-stream"
        }
    ];
}

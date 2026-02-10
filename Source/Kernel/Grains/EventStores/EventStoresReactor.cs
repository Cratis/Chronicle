// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grains.Events;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.EventStores;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles event store events.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to use.</param>
[Reactor(eventSequence: WellKnownEventSequences.EventLog)]
public class EventStoresReactor(IStorage storage) : Reactor
{
    /// <summary>
    /// Handles when a domain specification has been set for an event store.
    /// </summary>
    /// <param name="event">The event containing the domain specification information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task DomainSpecified(EventStoreDomainSpecified @event, EventContext eventContext)
    {
        await storage.SetDomainSpecification(@event.EventStore, @event.Specification);
    }
}

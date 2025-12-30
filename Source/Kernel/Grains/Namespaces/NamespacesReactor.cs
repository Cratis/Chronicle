// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

namespace Cratis.Chronicle.Grains.Namespaces;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles namespace events.
/// </summary>
[Reactor(eventSequence: WellKnownEventSequences.System)]
public class NamespacesReactor : Reactor
{
    /// <summary>
    /// Handles the addition of a namespace.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public Task Added(NamespaceAdded @event, EventContext eventContext)
    {
        // Handle the namespace added event
        // This could involve registering the namespace with reactors or other components
        return Task.CompletedTask;
    }
}

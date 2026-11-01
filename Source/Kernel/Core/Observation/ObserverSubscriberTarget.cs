// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a single connected client instance an observer subscription can deliver events to.
/// </summary>
/// <param name="SiloAddress">The <see cref="SiloAddress"/> of the silo terminating the client instance's connection.</param>
/// <param name="ConnectedClient">The <see cref="ConnectedClient"/> for the client instance, or null for kernel-owned subscribers.</param>
public record ObserverSubscriberTarget(SiloAddress SiloAddress, ConnectedClient? ConnectedClient = null)
{
    /// <summary>
    /// Gets the event types and generation pins declared by this target, or null for a legacy target.
    /// </summary>
    public IReadOnlyList<EventType>? DeclaredEventTypes { get; init; }

    /// <summary>
    /// Gets this target's generation delivery policy, or null for a legacy target.
    /// </summary>
    public EventGenerationDelivery? GenerationDelivery { get; init; }
}

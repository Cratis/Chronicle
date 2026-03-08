// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using NJsonSchema;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the event types in the system.
/// </summary>
public interface IEventTypes
{
    /// <summary>
    /// Gets the JSON schema for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>The JSON schema for the event type.</returns>
    JsonSchema GetJsonSchema(Type eventType);

    /// <summary>
    /// Gets the Clr type for the specified event type identifier.
    /// </summary>
    /// <param name="eventTypeId">The event type identifier.</param>
    /// <returns>The Clr type for the event type identifier.</returns>
    Type GetClrTypeFor(EventTypeId eventTypeId);

    /// <summary>
    /// Discovers and registers the event types for the kernel.
    /// </summary>
    /// <param name="eventStore">The event store to discover and register event types for.</param>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndRegister(Concepts.EventStoreName eventStore);
}

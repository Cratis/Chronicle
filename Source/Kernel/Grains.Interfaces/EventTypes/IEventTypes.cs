// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Grains.EventTypes.Kernel;

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
    /// Discovers and registers the event types for the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndRegister();
}

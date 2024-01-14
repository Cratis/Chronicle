// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Grains.EventTypes;

/// <summary>
/// Defines the store for event schemas.
/// </summary>
public interface IEventTypes : IGrainWithGuidKey
{
    /// <summary>
    /// Register a <see cref="JsonSchema"/> for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="type"><see cref="EventType"/> to register for.</param>
    /// <param name="friendlyName">A friendly name to identify the event with.</param>
    /// <param name="schema"><see cref="JsonSchema"/> to register.</param>
    /// /// <returns>Async task.</returns>
    Task Register(EventType type, string friendlyName, string schema);
}

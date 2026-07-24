// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Defines a per-event-store grain that evicts the local event type storage cache when an event type is
/// registered or changed anywhere in the cluster.
/// </summary>
public interface IEventTypesCacheInvalidator : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the invalidator so it is subscribed to the broadcast channel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();
}

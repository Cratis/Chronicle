// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Defines a client that evicts an event type from the storage cache on every silo in the cluster.
/// </summary>
public interface IEventTypesCacheClient
{
    /// <summary>
    /// Evict an event type from the storage cache on every silo in the cluster.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event type belongs to.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to evict.</param>
    /// <returns>Awaitable task.</returns>
    Task Invalidate(EventStoreName eventStore, EventTypeId eventTypeId);
}

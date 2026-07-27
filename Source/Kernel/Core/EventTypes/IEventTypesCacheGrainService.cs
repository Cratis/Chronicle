// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Services;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Defines a service that lives in each silo and evicts that silo's caches for a specific event type.
/// </summary>
public interface IEventTypesCacheGrainService : IGrainService
{
    /// <summary>
    /// Evict the local silo's caches for a specific <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event type belongs to.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to evict.</param>
    /// <returns>Awaitable task.</returns>
    Task Invalidate(EventStoreName eventStore, EventTypeId eventTypeId);
}

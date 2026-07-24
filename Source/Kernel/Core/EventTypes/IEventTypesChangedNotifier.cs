// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Defines a system that broadcasts event type registrations to every silo so their caches can evict.
/// </summary>
public interface IEventTypesChangedNotifier
{
    /// <summary>
    /// Notify every silo that an event type in an event store has been registered or changed.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event type belongs to.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> that changed.</param>
    /// <returns>Awaitable task.</returns>
    Task Notify(EventStoreName eventStore, EventTypeId eventTypeId);
}

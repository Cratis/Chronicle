// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the broadcast message telling every silo that an event type has been registered or changed.
/// </summary>
/// <param name="EventStore">Event store the event type belongs to.</param>
/// <param name="EventTypeId">The <see cref="Concepts.Events.EventTypeId"/> that changed.</param>
public record EventTypesChanged(EventStoreName EventStore, EventTypeId EventTypeId);

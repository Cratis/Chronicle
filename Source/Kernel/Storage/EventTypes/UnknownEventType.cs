// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// Exception that gets thrown when an event type is unknown.
/// </summary>
/// <param name="eventStore"><see cref="EventStoreName"/> the event type is missing from.</param>
/// <param name="type">The <see cref="EventTypeId"/> missing.</param>
public class UnknownEventType(EventStoreName eventStore, EventTypeId type) :
    Exception($"Event type '{type}' is unknown in the event store '{eventStore}'");

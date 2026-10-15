// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// The exception that is thrown when an event target names the event log as its destination.
/// </summary>
/// <param name="eventType">The <see cref="EventType"/> that was targeted.</param>
public class EventLogIsNotAPublicationTarget(EventType eventType) : Exception($"Event type '{eventType.Id}' cannot be published to the event log; public events are produced from private events into the outbox or another sequence.");

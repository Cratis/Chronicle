// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// The exception that is thrown when a projection or reducer publishes an event type that is not public to the outbox.
/// </summary>
/// <param name="eventType">The target event type.</param>
/// <param name="eventStore">The event store the client is connected to.</param>
public class PrivateEventTypeCannotBePublishedToOutbox(Type eventType, EventStoreName eventStore)
    : Exception($"Event type '{eventType.FullName}' is not public for event store '{eventStore.Value}' and cannot be published to the outbox. Adorn it with [Public] or declare it in a contracts assembly with [assembly: EventStore(\"{eventStore.Value}\")].");

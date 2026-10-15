// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// The exception that is thrown when a projection or reducer event target names the event log as its destination.
/// </summary>
/// <param name="eventType">The target event type.</param>
public class EventLogIsNotAPublicationTarget(Type eventType)
    : Exception($"Event type '{eventType.FullName}' cannot be published to the event log; public events are produced from private events into the outbox or another sequence.");

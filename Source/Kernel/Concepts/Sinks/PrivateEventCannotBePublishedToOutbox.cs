// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// The exception that is thrown when a private event type is the target of a publication to the outbox.
/// </summary>
/// <param name="eventType">The <see cref="EventType"/> that was targeted.</param>
public class PrivateEventCannotBePublishedToOutbox(EventType eventType) : Exception($"Event type '{eventType.Id}' is not declared public and cannot be published to the outbox.");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// The exception that is thrown when the exact event target has no registered schema.
/// </summary>
/// <param name="eventType">The requested event type.</param>
public class MissingEventTargetSchema(EventType eventType) : Exception($"No schema is registered for event target '{eventType}'.");

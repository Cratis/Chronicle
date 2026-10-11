// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer pins an unregistered event generation.
/// </summary>
/// <param name="eventType">The unregistered event type.</param>
public class EventTypeGenerationNotRegisteredForObserver(EventType eventType) : Exception($"Event type generation '{eventType}' is not registered for the observer.");

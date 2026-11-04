// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an event type would reach more than one method of an observer.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="eventType">The event <see cref="Type"/>.</param>
public class EventTypeObservedByMoreThanOneMethod(Type observerType, Type eventType)
    : Exception($"Event type '{eventType.FullName}' reaches more than one method of '{observerType.FullName}'. Each event type must reach exactly one method: a batch method or a per-event method, never both.");

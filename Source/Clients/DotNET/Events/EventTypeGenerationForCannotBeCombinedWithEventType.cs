// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// The exception that is thrown when a type carries both <see cref="EventTypeAttribute"/> and
/// <see cref="EventTypeGenerationForAttribute"/>.
/// </summary>
/// <param name="type">The type carrying both attributes.</param>
public class EventTypeGenerationForCannotBeCombinedWithEventType(Type type)
    : Exception($"'{type.Name}' is marked with both [EventType] and [EventTypeGenerationFor<T>]. A type is either the current event type or a previous generation of one, not both.");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// The exception that is thrown when a type marked with <see cref="EventTypeGenerationForAttribute"/> references
/// a type that is not itself marked with <see cref="EventTypeAttribute"/>.
/// </summary>
/// <param name="generationType">The type marked with <see cref="EventTypeGenerationForAttribute"/>.</param>
/// <param name="referencedType">The referenced type that is not an event type.</param>
public class EventTypeGenerationReferencesNonEventType(Type generationType, Type referencedType)
    : Exception($"'{generationType.Name}' declares itself as a generation for '{referencedType.Name}', but '{referencedType.Name}' is not marked with [EventType]. The type referenced by [EventTypeGenerationFor<T>] must carry [EventType] directly.");

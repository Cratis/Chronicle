// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents what event type discovery produced, so it can be adopted by another registry over an equal catalog.
/// </summary>
/// <param name="Descriptors">The descriptors by CLR type. Frozen, so safe to share.</param>
/// <param name="TypesByEventType">The CLR types by event type. Frozen, so safe to share.</param>
/// <param name="Schemas">The schemas by event type. Mutable, so every adopter takes its own copy.</param>
internal sealed record EventTypesDiscovery(
    FrozenDictionary<Type, EventTypeDescriptor> Descriptors,
    FrozenDictionary<EventType, Type> TypesByEventType,
    FrozenDictionary<EventType, JsonSchema> Schemas);

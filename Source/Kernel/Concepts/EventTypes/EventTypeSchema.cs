// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using NJsonSchema;

namespace Cratis.Chronicle.Concepts.EventTypes;

/// <summary>
/// Represents the schema of an event.
/// </summary>
/// <param name="Type">The <see cref="EventType">type of event</see>.</param>
/// <param name="Owner">The <see cref="EventTypeOwner">owner</see> of the event type.</param>
/// <param name="Source">The <see cref="EventTypeSource">source</see> of the event type.</param>
/// <param name="Schema">The <see cref="JsonSchema">JSON schema</see>.</param>
public record EventTypeSchema(EventType Type, EventTypeOwner Owner, EventTypeSource Source, JsonSchema Schema);

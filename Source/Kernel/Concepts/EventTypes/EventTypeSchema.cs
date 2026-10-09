// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Targets;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.EventTypes;

/// <summary>
/// Represents the schema of an event.
/// </summary>
/// <param name="Type">The <see cref="EventType">type of event</see>.</param>
/// <param name="Owner">The <see cref="EventTypeOwner">owner</see> of the event type.</param>
/// <param name="Source">The <see cref="EventTypeSource">source</see> of the event type.</param>
/// <param name="Schema">The <see cref="JsonSchema">JSON schema</see>.</param>
/// <param name="Visibility">The <see cref="EventTypeVisibility">visibility</see> of the event type. Unspecified for event types registered by clients that predate visibility.</param>
/// <param name="Origin">The name of the event store the event type originates from when it is not the registering one, otherwise empty.</param>
public record EventTypeSchema(
    EventType Type,
    EventTypeOwner Owner,
    EventTypeSource Source,
    JsonSchema Schema,
    EventTypeVisibility Visibility = EventTypeVisibility.Unspecified,
    string Origin = "") : IHaveTargetSchema
{
    /// <inheritdoc/>
    public JsonSchema GetTargetSchema() => Schema;
}

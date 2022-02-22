// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Events.Schemas;

/// <summary>
/// Represents the schema of an event.
/// </summary>
/// <param name="Type">The <see cref="EventType">type of event</see>.</param>
/// <param name="FriendlyName">A friendly name for the event.</param>
/// <param name="Schema">The <see cref="JsonSchema">JSON schema</see>.</param>
public record EventSchemaDefinition(EventType Type, string FriendlyName, JsonSchema Schema);

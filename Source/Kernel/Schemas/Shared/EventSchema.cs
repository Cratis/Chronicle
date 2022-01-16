// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents the schema of an event.
    /// </summary>
    /// <param name="Type">The <see cref="EventType">type of event</see>.</param>
    /// <param name="Schema">The <see cref="JsonSchema">JSON schema</see>.</param>
    public record EventSchema(EventType Type, JsonSchema Schema);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents the schema of an event.
    /// </summary>
    /// <param name="Type">The <see cref="EventType">type of event</see>-</param>
    /// <param name="FriendlyName">A friendly name for the event.</param>
    /// <param name="Schema">The <see cref="JSchema">JSON schema</see>.</param>
    public record EventSchemaDefinition(EventType Type, string FriendlyName, JSchema Schema);
}

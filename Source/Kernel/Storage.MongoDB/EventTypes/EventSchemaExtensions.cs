// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Events.MongoDB.EventTypes;

/// <summary>
/// Extension methods for working with <see cref="EventTypeSchema"/>.
/// </summary>
public static class EventSchemaExtensions
{
    /// <summary>
    /// Convert to a <see cref="EventType">MongoDB</see> representation.
    /// </summary>
    /// <param name="schema"><see cref="EventTypeSchema"/> to convert.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static EventType ToMongoDB(this EventTypeSchema schema)
    {
        return new EventType(
            schema.Type.Id,
            EventTypeOwner.Client,
            schema.Type.Tombstone,
            new Dictionary<EventTypeGeneration, string>
            {
                { schema.Type.Generation, schema.Schema.ToJson() }
            });
    }

    /// <summary>
    /// Convert to <see cref="EventTypeSchema"/> from <see cref="EventType"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeSchema"/>.</returns>
    public static EventTypeSchema ToEventSchema(this EventType schema)
    {
        var result = JsonSchema.FromJsonAsync(schema.Schemas.First().Value).GetAwaiter().GetResult();
        result.EnsureComplianceMetadata();
        result.ResetFlattenedProperties();
        result.EnsureFlattenedProperties();

        return new EventTypeSchema(
            new Chronicle.Concepts.Events.EventType(
               schema.Id,
               EventTypeGeneration.First,
               schema.Tombstone),
            result);
    }
}

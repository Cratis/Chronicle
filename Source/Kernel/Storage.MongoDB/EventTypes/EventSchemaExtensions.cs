// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Events.MongoDB.EventTypes;

/// <summary>
/// Extension methods for working with <see cref="EventTypeSchema"/>.
/// </summary>
public static class EventSchemaExtensions
{
    /// <summary>
    /// Convert to a <see cref="EventSchemaMongoDB">MongoDB</see> representation.
    /// </summary>
    /// <param name="schema"><see cref="EventTypeSchema"/> to convert.</param>
    /// <returns>Converted <see cref="EventSchemaMongoDB"/>.</returns>
    public static EventSchemaMongoDB ToMongoDB(this EventTypeSchema schema)
    {
        return new EventSchemaMongoDB
        {
            EventType = schema.Type.Id,
            Generation = schema.Type.Generation,
            Schema = schema.Schema.ToJson()
        };
    }

    /// <summary>
    /// Convert to <see cref="EventTypeSchema"/> from <see cref="EventSchemaMongoDB"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventSchemaMongoDB"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeSchema"/>.</returns>
    public static EventTypeSchema ToEventSchema(this EventSchemaMongoDB schema)
    {
        var result = JsonSchema.FromJsonAsync(schema.Schema).GetAwaiter().GetResult();
        result.EnsureComplianceMetadata();
        result.ResetFlattenedProperties();
        result.EnsureFlattenedProperties();

        return new EventTypeSchema(
            new EventType(
               schema.EventType,
               schema.Generation),
            result);
    }
}

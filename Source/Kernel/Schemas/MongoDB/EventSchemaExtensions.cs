// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Schemas;
using NJsonSchema;

namespace Aksio.Cratis.Events.Schemas.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="EventSchema"/>.
/// </summary>
public static class EventSchemaExtensions
{
    /// <summary>
    /// Convert to a <see cref="EventSchemaMongoDB">MongoDB</see> representation.
    /// </summary>
    /// <param name="schema"><see cref="EventSchema"/> to convert.</param>
    /// <returns>Converted <see cref="EventSchemaMongoDB"/>.</returns>
    public static EventSchemaMongoDB ToMongoDB(this EventSchema schema)
    {
        return new EventSchemaMongoDB
        {
            EventType = schema.Type.Id,
            Generation = schema.Type.Generation,
            Schema = schema.Schema.ToJson()
        };
    }

    /// <summary>
    /// Convert to <see cref="EventSchema"/> from <see cref="EventSchemaMongoDB"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventSchemaMongoDB"/> to convert from.</param>
    /// <returns>Converted <see cref="EventSchema"/>.</returns>
    public static EventSchema ToEventSchema(this EventSchemaMongoDB schema)
    {
        var task = JsonSchema.FromJsonAsync(schema.Schema);
        task.Wait();

        task.Result.EnsureCorrectMetadata();

        return new EventSchema(
            new EventType(
               schema.EventType,
               schema.Generation),
            task.Result);
    }
}

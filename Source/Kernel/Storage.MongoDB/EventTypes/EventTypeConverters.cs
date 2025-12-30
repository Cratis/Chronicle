// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Converter methods for working with <see cref="EventTypeSchema"/> converting to and from MongoDB representations.
/// </summary>
public static class EventTypeConverters
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
            schema.Owner,
            schema.Source,
            schema.Type.Tombstone,
            new Dictionary<string, BsonDocument>
            {
                { schema.Type.Generation.ToString(), BsonDocument.Parse(schema.Schema.ToJson()) }
            });
    }

    /// <summary>
    /// Convert to <see cref="EventTypeSchema"/> from <see cref="EventType"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeSchema"/>.</returns>
    public static EventTypeSchema ToKernel(this EventType schema)
    {
        var result = JsonSchema.FromJsonAsync(schema.Schemas.First().Value.ToJson()).GetAwaiter().GetResult();
        result.EnsureComplianceMetadata();

        return new EventTypeSchema(
            new Concepts.Events.EventType(
               schema.Id,
               EventTypeGeneration.First,
               schema.Tombstone),
            schema.Owner,
            schema.Source,
            result);
    }
}

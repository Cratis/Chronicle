// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sql.ReadModels;

/// <summary>
/// Converter methods for working with <see cref="EventTypeSchema"/> converting to and from SQL representations.
/// </summary>
public static class ReadModelDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="EventType">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="ReadModelDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static ReadModel ToSql(this ReadModelDefinition definition) =>
        new()
        {
            Id = definition.Name,
            Owner = definition.Owner,
            Schemas = definition.Schemas.ToDictionary(kvp => (uint)kvp.Key, kvp => kvp.Value.ToJson())
        };

    /// <summary>
    /// Convert to <see cref="EventTypeSchema"/> from <see cref="EventType"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeSchema"/>.</returns>
    public static ReadModelDefinition ToKernel(this ReadModel schema) =>
        new(
            schema.Id,
            schema.Owner,
            schema.Schemas.ToDictionary(kvp => (ReadModelGeneration)kvp.Key, kvp => JsonSchema.FromJsonAsync(kvp.Value).GetAwaiter().GetResult()));
}

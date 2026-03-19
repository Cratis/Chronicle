// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;

/// <summary>
/// Converter methods for working with <see cref="Concepts.ReadModels.ReadModelDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ReadModelDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="ReadModelDefinition">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="Concepts.ReadModels.ReadModelDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ReadModelDefinition"/>.</returns>
    public static ReadModelDefinition ToSql(this Concepts.ReadModels.ReadModelDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Name = definition.ContainerName,
            Owner = definition.Owner,
            Schemas = definition.Schemas.ToDictionary(kvp => (uint)kvp.Key, kvp => kvp.Value.ToJson())
        };

    /// <summary>
    /// Convert to <see cref="Concepts.ReadModels.ReadModelDefinition"/> from <see cref="ReadModelDefinition"/>.
    /// </summary>
    /// <param name="schema"><see cref="ReadModelDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="Concepts.ReadModels.ReadModelDefinition"/>.</returns>
    public static Concepts.ReadModels.ReadModelDefinition ToKernel(this ReadModelDefinition schema) =>
        new(
            schema.Id,
            schema.Name,
            ReadModelDisplayName.NotSet,
            schema.Owner,
            ReadModelSource.Unknown,
            ReadModelObserverType.NotSet,
            ReadModelObserverIdentifier.Unspecified,
            Concepts.Sinks.SinkDefinition.None,
            schema.Schemas.ToDictionary(kvp => (ReadModelGeneration)kvp.Key, kvp => JsonSchema.FromJsonAsync(kvp.Value).GetAwaiter().GetResult()),
            []);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Converter methods for <see cref="ReadModelDefinition"/>.
/// </summary>
internal static class ReadModelDefinitionConverters
{
    /// <summary>
    /// Converts a <see cref="ReadModelDefinition"/> to a <see cref="Contracts.ReadModels.ReadModelDefinition"/>.
    /// </summary>
    /// <param name="definition">The <see cref="ReadModelDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.ReadModels.ReadModelDefinition"/>.</returns>
    public static Contracts.ReadModels.ReadModelDefinition ToContract(this ReadModelDefinition definition)
    {
        var latestGeneration = definition.LatestGeneration;
        var latestSchema = definition.GetSchemaForLatestGeneration();
        return new()
        {
            Identifier = definition.Identifier,
            Name = definition.Name,
            Generation = latestGeneration.Value,
            Schema = latestSchema.ToJson(),
            Indexes = definition.Indexes.Select(i => new Contracts.ReadModels.IndexDefinition { PropertyPath = i.PropertyPath.Path }).ToList()
        };
    }

    /// <summary>
    /// Converts a <see cref="Contracts.ReadModels.ReadModelDefinition"/> to a <see cref="ReadModelDefinition"/>.
    /// </summary>
    /// <param name="contract">The <see cref="Contracts.ReadModels.ReadModelDefinition"/> to convert.</param>
    /// <param name="owner">The owner of the read model.</param>
    /// <returns>The converted <see cref="ReadModelDefinition"/>.</returns>
    public static ReadModelDefinition ToChronicle(this Contracts.ReadModels.ReadModelDefinition contract, Contracts.ReadModels.ReadModelOwner owner)
    {
        var schema = JsonSchema.FromJsonAsync(contract.Schema).GetAwaiter().GetResult();
        var indexes = contract.Indexes
            .Select(i => new IndexDefinition(new PropertyPath(i.PropertyPath)))
            .ToArray();

        return new(
            contract.Identifier,
            contract.Name,
            (ReadModelOwner)(int)owner,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { (ReadModelGeneration)contract.Generation, schema }
            },
            indexes
        );
    }
}

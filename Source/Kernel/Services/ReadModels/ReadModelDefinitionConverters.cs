// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
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
            Name = definition.Name,
            Owner = (Contracts.ReadModels.ReadModelOwner)(int)definition.Owner,
            Generation = latestGeneration.Value,
            Schema = latestSchema.ToJson()
        };
    }

    /// <summary>
    /// Converts a <see cref="Contracts.ReadModels.ReadModelDefinition"/> to a <see cref="ReadModelDefinition"/>.
    /// </summary>
    /// <param name="contract">The <see cref="Contracts.ReadModels.ReadModelDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="ReadModelDefinition"/>.</returns>
    public static ReadModelDefinition ToChronicle(this Contracts.ReadModels.ReadModelDefinition contract)
    {
        var schema = JsonSchema.FromJsonAsync(contract.Schema).GetAwaiter().GetResult();

        return new(
            contract.Name,
            (ReadModelOwner)(int)contract.Owner,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { (ReadModelGeneration)contract.Generation, schema }
            }
        );
    }
}

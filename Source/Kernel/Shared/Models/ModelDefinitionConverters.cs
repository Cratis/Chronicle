// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Models;

/// <summary>
/// Converter methods for <see cref="ModelDefinition"/>.
/// </summary>
public static class ModelDefinitionConverters
{
    /// <summary>
    /// Converts a <see cref="ModelDefinition"/> to a <see cref="Contracts.Models.ModelDefinition"/>.
    /// </summary>
    /// <param name="definition">The <see cref="ModelDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Models.ModelDefinition"/>.</returns>
    public static Contracts.Models.ModelDefinition ToContract(this ModelDefinition definition)
    {
        return new()
        {
            Name = definition.Name,
            Schema = definition.Schema
        };
    }

    /// <summary>
    /// Converts a <see cref="Contracts.Models.ModelDefinition"/> to a <see cref="ModelDefinition"/>.
    /// </summary>
    /// <param name="contract">The <see cref="Contracts.Models.ModelDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="ModelDefinition"/>.</returns>
    public static ModelDefinition ToChronicle(this Contracts.Models.ModelDefinition contract)
    {
        return new(
            contract.Name,
            contract.Schema
        );
    }
}

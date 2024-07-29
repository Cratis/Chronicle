// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="AllDefinition"/>.
/// </summary>
public static class AllDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="AllDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="AllDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.AllDefinition ToContract(this AllDefinition definition)
    {
        return new()
        {
            Properties = definition.Properties.ToDictionary(_ => (string)_.Key, _ => _.Value),
            IncludeChildren = definition.IncludeChildren
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="AllDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.AllDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static AllDefinition ToChronicle(this Contracts.Projections.AllDefinition contract)
    {
        return new(
            contract.Properties.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value),
            contract.IncludeChildren
        );
    }
}

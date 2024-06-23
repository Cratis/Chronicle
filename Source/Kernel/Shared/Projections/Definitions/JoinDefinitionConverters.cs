// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="JoinDefinition"/>.
/// </summary>
public static class JoinDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="JoinDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="JoinDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.JoinDefinition ToContract(this JoinDefinition definition)
    {
        return new()
        {
            On = definition.On,
            Properties = definition.Properties.ToDictionary(_ => (string)_.Key, _ => _.Value),
            Key = definition.Key
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="JoinDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.JoinDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static JoinDefinition ToChronicle(this Contracts.Projections.JoinDefinition contract)
    {
        return new(
            contract.On,
            contract.Properties.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value),
            contract.Key
        );
    }
}

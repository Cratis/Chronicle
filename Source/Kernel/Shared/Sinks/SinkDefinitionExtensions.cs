// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="SinkDefinition"/>.
/// </summary>
public static class SinkDefinitionExtensions
{
    /// <summary>
    /// Convert to contract version of <see cref="SinkDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="SinkDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Sinks.SinkDefinition ToContract(this SinkDefinition definition)
    {
        return new()
        {
            ConfigurationId = definition.ConfigurationId,
            TypeId = definition.TypeId
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="SinkDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Sinks.SinkDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static SinkDefinition ToChronicle(this Contracts.Sinks.SinkDefinition contract)
    {
        return new(new SinkConfigurationId(contract.ConfigurationId), contract.TypeId);
    }
}

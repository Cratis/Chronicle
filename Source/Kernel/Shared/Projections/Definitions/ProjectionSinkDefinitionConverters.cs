// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converters for <see cref="ProjectionSinkDefinition"/>.
/// </summary>
public static class ProjectionSinkDefinitionConverters
{
    /// <summary>
    /// Convert from a <see cref="ProjectionSinkDefinition"/> to <see cref="Contracts.Projections.ProjectionSinkDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionSinkDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="Contracts.Projections.ProjectionSinkDefinition"/>.</returns>
    public static Contracts.Projections.ProjectionSinkDefinition ToContract(this ProjectionSinkDefinition definition)
    {
        return new()
        {
            ConfigurationId = definition.ConfigurationId,
            TypeId = definition.TypeId
        };
    }

    /// <summary>
    /// Convert from a <see cref="Contracts.Projections.ProjectionSinkDefinition"/> to <see cref="ProjectionSinkDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="Contracts.Projections.ProjectionSinkDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionSinkDefinition"/>.</returns>
    public static ProjectionSinkDefinition ToChronicle(this Contracts.Projections.ProjectionSinkDefinition definition)
    {
        return new(definition.ConfigurationId, definition.TypeId);
    }
}

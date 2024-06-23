// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converters for <see cref="ProjectionPipelineDefinition"/>.
/// </summary>
public static class ProjectionPipelineDefinitionConverters
{
    /// <summary>
    /// Convert from a <see cref="ProjectionPipelineDefinition"/> to <see cref="Contracts.Projections.ProjectionPipelineDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionPipelineDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="Contracts.Projections.ProjectionPipelineDefinition"/>.</returns>
    public static Contracts.Projections.ProjectionPipelineDefinition ToContract(this ProjectionPipelineDefinition definition)
    {
        return new()
        {
            ProjectionId = definition.ProjectionId.ToString(),
            Sinks = definition.Sinks.Select(_ => _.ToContract())
        };
    }

    /// <summary>
    /// Convert from a <see cref="Contracts.Projections.ProjectionPipelineDefinition"/> to <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="Contracts.Projections.ProjectionPipelineDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionPipelineDefinition"/>.</returns>
    public static ProjectionPipelineDefinition ToChronicle(this Contracts.Projections.ProjectionPipelineDefinition definition)
    {
        return new(definition.ProjectionId, definition.Sinks.Select(_ => _.ToChronicle()));
    }
}

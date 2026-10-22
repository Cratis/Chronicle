// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Sinks;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Decides whether a projection can be built as a materialization generation, before anything is allocated or written.
/// </summary>
public static class MaterializationGenerationCapabilities
{
    /// <summary>
    /// Check whether a projection can be built as a materialization generation.
    /// </summary>
    /// <param name="scope">The <see cref="ProjectionScope"/> of the projection.</param>
    /// <param name="sinkType">The <see cref="SinkTypeId"/> the projection writes to.</param>
    /// <returns>True if it can, false if not.</returns>
    public static bool CanBuild(ProjectionScope scope, SinkTypeId sinkType) =>
        scope == ProjectionScope.Namespaced && sinkType != WellKnownSinkTypes.EventSequence;

    /// <summary>
    /// Throw if a projection cannot build the given generation.
    /// </summary>
    /// <param name="projection">The <see cref="EngineProjection"/> to check.</param>
    /// <param name="generation">The <see cref="MaterializationGenerationIdentity"/> it is to build.</param>
    /// <exception cref="GenerationBelongsToAnotherProjection">Thrown when the generation belongs to another projection.</exception>
    /// <exception cref="GlobalProjectionCannotBuildGenerations">Thrown when the projection is not namespaced.</exception>
    /// <exception cref="SinkCannotBuildGenerations">Thrown when the projection's sink cannot be built privately.</exception>
    public static void ThrowIfCannotBuild(EngineProjection projection, MaterializationGenerationIdentity generation)
    {
        if (generation.ProjectionId != projection.Identifier)
        {
            throw new GenerationBelongsToAnotherProjection(projection.Identifier, generation);
        }

        if (projection.Scope != ProjectionScope.Namespaced)
        {
            throw new GlobalProjectionCannotBuildGenerations(projection.Identifier, projection.Scope);
        }

        if (projection.ReadModel.Sink.Type == WellKnownSinkTypes.EventSequence)
        {
            throw new SinkCannotBuildGenerations(projection.Identifier, projection.ReadModel.Sink.Type);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter for converting between <see cref="ProjectionAndPipeline"/> and <see cref="Contracts.Projections.ProjectionAndPipeline"/>.
/// </summary>
public static class ProjectionAndPipelineConverters
{
    /// <summary>
    /// Convert from <see cref="ProjectionAndPipeline"/> to <see cref="Contracts.Projections.ProjectionAndPipeline"/>.
    /// </summary>
    /// <param name="projectionAndPipeline"><see cref="ProjectionAndPipeline"/> to convert from.</param>
    /// <returns>Converted <see cref="Contracts.Projections.ProjectionAndPipeline"/> instance.</returns>
    public static Contracts.Projections.ProjectionAndPipeline ToContract(this ProjectionAndPipeline projectionAndPipeline)
    {
        return new()
        {
            Projection = projectionAndPipeline.Projection.ToContract(),
            Pipeline = projectionAndPipeline.Pipeline.ToContract()
        };
    }

    /// <summary>
    /// Convert from <see cref="Contracts.Projections.ProjectionAndPipeline"/> to <see cref="ProjectionAndPipeline"/>.
    /// </summary>
    /// <param name="projectionAndPipeline"><see cref="Contracts.Projections.ProjectionAndPipeline"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionAndPipeline"/> instance.</returns>
    public static ProjectionAndPipeline ToChronicle(this Contracts.Projections.ProjectionAndPipeline projectionAndPipeline)
    {
        return new(
            projectionAndPipeline.Projection.ToChronicle(),
            projectionAndPipeline.Pipeline.ToChronicle()
        );
    }
}

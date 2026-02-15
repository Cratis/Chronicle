// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Defines what is needed to perform a step in the projection pipeline.
/// </summary>
public interface ICanPerformProjectionPipelineStep
{
    /// <summary>
    /// Perform the step.
    /// </summary>
    /// <param name="projection"><see cref="EngineProjection"/> to perform for.</param>
    /// <param name="context">The <see cref="ProjectionEventContext"/> to perform in.</param>
    /// <returns>Possibly a mutated <see cref="ProjectionEventContext"/>.</returns>
    ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context);
}

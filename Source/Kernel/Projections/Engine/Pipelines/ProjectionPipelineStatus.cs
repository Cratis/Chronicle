// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Represents the status that can be observed for a pipeline.
/// </summary>
/// <param name="Projection"><see cref="IProjection"/> the status is for.</param>
/// <param name="State"><see cref="ProjectionState"/>.</param>
/// <param name="Positions">Positions for all configurations.</param>
/// <param name="Jobs">All Jobs on the pipeline.</param>
public record ProjectionPipelineStatus(IProjection? Projection, ProjectionState State, IDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> Positions, IEnumerable<IProjectionPipelineJob> Jobs)
{
    /// <summary>
    /// The initial status of a pipeline.
    /// </summary>
    public static readonly ProjectionPipelineStatus Initial = new(default, ProjectionState.Unknown, new Dictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>(), Array.Empty<IProjectionPipelineJob>());
}

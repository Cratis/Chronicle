// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Defines a system for creating pipeline jobs.
/// </summary>
public interface IProjectionPipelineJobs
{
    /// <summary>
    /// Creates a rewind job for a specific storage configurations.
    /// </summary>
    /// <param name="pipeline"><see cref="IProjectionPipeline"/> to create for.</param>
    /// <param name="configurationId">Optional configuration to rewind. If none is given it will rewind all.</param>
    /// <returns><see cref="IProjectionPipelineJob"/> for rewinding.</returns>
    IProjectionPipelineJob Rewind(IProjectionPipeline pipeline, ProjectionResultStoreConfigurationId configurationId);

    /// <summary>
    /// Creates rewind jobs for all storage configurations.
    /// </summary>
    /// <param name="pipeline"><see cref="IProjectionPipeline"/> to create for.</param>
    /// <returns>Collection of <see cref="IProjectionPipelineJob"/>.</returns>
    IEnumerable<IProjectionPipelineJob> Rewind(IProjectionPipeline pipeline);

    /// <summary>
    /// Creates a catchup job for a specific storage configurations.
    /// </summary>
    /// <param name="pipeline"><see cref="IProjectionPipeline"/> to create for.</param>
    /// <param name="configurationId">Optional configuration to rewind. If none is given it will rewind all.</param>
    /// <returns><see cref="IProjectionPipelineJob"/> for catching up.</returns>
    IProjectionPipelineJob Catchup(IProjectionPipeline pipeline, ProjectionResultStoreConfigurationId configurationId);

    /// <summary>
    /// Creates a catchup job for all storage configurations.
    /// </summary>
    /// <param name="pipeline"><see cref="IProjectionPipeline"/> to create for.</param>
    /// <returns>Collection of <see cref="IProjectionPipelineJob"/> for catching up.</returns>
    IEnumerable<IProjectionPipelineJob> Catchup(IProjectionPipeline pipeline);
}

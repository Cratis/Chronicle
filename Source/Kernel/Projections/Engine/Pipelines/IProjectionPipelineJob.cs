// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Defines a job that can be executed on a <see cref="IProjectionPipeline"/>.
/// </summary>
public interface IProjectionPipelineJob
{
    /// <summary>
    /// Gets the name of the job.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the <see cref="ProjectionPipelineJobStatus"/>.
    /// </summary>
    ProjectionPipelineJobStatus Status { get; }

    /// <summary>
    /// Gets the collection of <see cref="IProjectionPipelineJobStep">steps</see>.
    /// </summary>
    IEnumerable<IProjectionPipelineJobStep> Steps { get; }

    /// <summary>
    /// Run the job.
    /// </summary>
    /// <returns>Async continuation.</returns>
    Task Run();

    /// <summary>
    /// Stops a running job.
    /// </summary>
    /// <returns>Async continuation.</returns>
    Task Stop();
}

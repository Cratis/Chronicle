// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Defines a system for handling events for a <see cref="IProjectionPipeline"/>.
/// </summary>
public interface IProjectionPipelineHandler
{
    /// <summary>
    /// Gets an observable of the position within the event log for each sink configuration.
    /// </summary>
    IObservable<IReadOnlyDictionary<ProjectionSinkConfigurationId, EventSequenceNumber>> Positions { get; }

    /// <summary>
    /// Gets the current positions within the event log for each sink configuration.
    /// </summary>
    IReadOnlyDictionary<ProjectionSinkConfigurationId, EventSequenceNumber> CurrentPositions { get; }

    /// <summary>
    /// Initializes the handler for a specific <see cref="IProjectionPipeline"/> and <see cref="ProjectionSinkConfigurationId"/>.
    /// </summary>
    /// <param name="pipeline"><see cref="IProjectionPipeline"/> it should handle for.</param>
    /// <param name="configurationId">What is the identifier for the projection sink.</param>
    /// <returns>Async task continuation.</returns>
    Task InitializeFor(IProjectionPipeline pipeline, ProjectionSinkConfigurationId configurationId);

    /// <summary>
    /// Handle event for pipeline.
    /// </summary>
    /// <param name="event">Event to handle.</param>
    /// <param name="pipeline"><see cref="IProjectionPipeline"/> it should handle for.</param>
    /// <param name="sink">Which <see cref="IProjectionSink"/> it should use.</param>
    /// <param name="configurationId">What is the identifier for the projection sink.</param>
    /// <returns>Next<see cref="EventSequenceNumber"/> it can handle.</returns>
    Task<EventSequenceNumber> Handle(AppendedEvent @event, IProjectionPipeline pipeline, IProjectionSink sink, ProjectionSinkConfigurationId configurationId);
}

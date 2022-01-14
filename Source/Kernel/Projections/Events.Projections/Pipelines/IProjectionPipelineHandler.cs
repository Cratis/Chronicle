// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Defines a system for handling events for a <see cref="IProjectionPipeline"/>.
    /// </summary>
    public interface IProjectionPipelineHandler
    {
        /// <summary>
        /// Gets an observable of the position within the event log for each result store configuration.
        /// </summary>
        IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions { get; }

        /// <summary>
        /// Gets the current positions within the event log for each result store configuration.
        /// </summary>
        IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> CurrentPositions { get; }

        /// <summary>
        /// Initializes the handler for a specific <see cref="IProjectionPipeline"/> and <see cref="ProjectionResultStoreConfigurationId"/>.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> it should handle for.</param>
        /// <param name="configurationId">What is the identifier for the projection result store.</param>
        /// <returns>Async task continuation.</returns>
        Task InitializeFor(IProjectionPipeline pipeline, ProjectionResultStoreConfigurationId configurationId);

        /// <summary>
        /// Handle event for pipeline.
        /// </summary>
        /// <param name="event">Event to handle.</param>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> it should handle for.</param>
        /// <param name="resultStore">Which <see cref="IProjectionResultStore"/> it should use.</param>
        /// <param name="configurationId">What is the identifier for the projection result store.</param>
        /// <returns>Next<see cref="EventLogSequenceNumber"/> it can handle.</returns>
        Task<EventLogSequenceNumber> Handle(Event @event, IProjectionPipeline pipeline, IProjectionResultStore resultStore, ProjectionResultStoreConfigurationId configurationId);
    }
}

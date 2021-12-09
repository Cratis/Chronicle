// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Defines a system that can coordinate the effort around projections.
    /// </summary>
    public interface IProjectionPipeline
    {
        /// <summary>
        /// Gets the <see cref="IProjection"/> the pipeline is for.
        /// </summary>
        IProjection Projection { get; }

        /// <summary>
        /// Gets the <see cref="IProjectionEventProvider"/> used in the pipeline.
        /// </summary>
        IProjectionEventProvider EventProvider { get; }

        /// <summary>
        /// Gets the <see cref="IProjectionResultStore">result stores</see> to use for output.
        /// </summary>
        IDictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore> ResultStores { get; }

        /// <summary>
        /// Gets an <see cref="IObservable{T}"/> of <see cref="ProjectionState">state</see> of the projection.
        /// </summary>
        IObservable<ProjectionState> State { get; }

        /// <summary>
        /// Gets an <see cref="IObservable{T}"/> of a collection of any <see cref="IProjectionPipelineJob"/> running.
        /// </summary>
        IObservable<IEnumerable<IProjectionPipelineJob>> Jobs { get; }

        /// <summary>
        /// Gets the current <see cref="ProjectionState"/>.
        /// </summary>
        ProjectionState CurrentState { get; }

        /// <summary>
        /// Gets an observable of the position within the event log for each result store configuration.
        /// </summary>
        IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions { get; }

        /// <summary>
        /// Starts the pipeline.
        /// </summary>
        /// <returns>A Task for async operations.</returns>
        Task Start();

        /// <summary>
        /// Resumes the pipeline if paused.
        /// </summary>
        /// <returns>A Task for async operations.</returns>
        Task Resume();

        /// <summary>
        /// Pause the pipeline.
        /// </summary>
        /// <returns>A Task for async operations.</returns>
        Task Pause();

        /// <summary>
        /// Rewind the entire pipeline for all the result stores.
        /// </summary>
        /// <returns>A Task for async operations.</returns>
        Task Rewind();

        /// <summary>
        /// Suspend a pipeline with a reason.
        /// </summary>
        /// <param name="reason">The reason for the suspension.</param>
        /// <returns>A Task for async operations.</returns>
        Task Suspend(string reason);

        /// <summary>
        /// Rewind the entire pipeline for a specific result store based on the unique identifier.
        /// </summary>
        /// <param name="configurationId"><see cref="ProjectionResultStoreConfigurationId"/> to rewind.</param>
        /// <returns>A Task for async operations.</returns>
        Task Rewind(ProjectionResultStoreConfigurationId configurationId);

        /// <summary>
        /// Adds a <see cref="IProjectionResultStore"/> for storing results.
        /// </summary>
        /// <param name="configurationId">The unique configuration identifier.</param>
        /// <param name="resultStore"><see cref="IProjectionResultStore">Storage provider</see> to add.</param>
        /// <remarks>
        /// One can have the output of a projection stored in multiple locations. It will treat every
        /// location separately with regards to intermediate results and all. The offset within the source is
        /// also unique per configuration.
        /// </remarks>
        void StoreIn(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system that can coordinate the effort around projections.
    /// </summary>
    public interface IProjectionPipeline
    {
        /// <summary>
        /// Gets the <see cref="IProjectionEventProvider"/> used in the pipeline.
        /// </summary>
        IProjectionEventProvider EventProvider { get; }

        /// <summary>
        /// Gets the <see cref="IProjectionStorage">storage providers</see> to use for output.
        /// </summary>
        IEnumerable<IProjectionStorage> StorageProviders { get; }

        /// <summary>
        /// Starts the pipeline.
        /// </summary>
        void Start();

        /// <summary>
        /// Resumes the pipeline if paused.
        /// </summary>
        void Resume();

        /// <summary>
        /// Pause the pipeline.
        /// </summary>
        void Pause();

        /// <summary>
        /// Gets the <see cref="IProjection"/> the pipeline is for.
        /// </summary>
        IProjection Projection { get; }

        /// <summary>
        /// Adds a <see cref="IProjectionStorage"/> for storing results.
        /// </summary>
        /// <param name="storageProvider"><see cref="IProjectionStorage">Storage provider</see> to add.</param>
        /// <remarks>
        /// One can have the output of a projection stored in multiple locations. It will treat every
        /// location separately with regards to intermediate results and all.
        /// </remarks>
        void StoreIn(IProjectionStorage storageProvider);
    }
}

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
        IProjectionEventProvider    EventProvider { get; }

        /// <summary>
        /// Provides the projection with a new <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> to provide.</param>
        /// <return>Async Task containing <see cref="Changeset"/> as result.</return>
        Task<Changeset> OnNext(Event @event);

        /// <summary>
        /// Adds a <see cref="IProjectionStorage"/> for storing results.
        /// </summary>
        /// <param name="storage"><see cref="IProjectionStorage"/> to use.</param>
        void StoreIn(IProjectionStorage storage);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a provider of events for <see cref="IProjection">projections</see>.
    /// </summary>
    public interface IProjectionEventProvider
    {
        /// <summary>
        /// Start providing events for a <see cref="IProjection"/>.
        /// </summary>
        /// <param name="projection"><see cref="IProjection"/> to start providing for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// The provider will provide events from the current position it has recorded for the
        /// <see cref="IProjection"/>. It will be in a state of catching up till its at the
        /// head of the stream. Once at the head, it will provide events as they occur.
        /// </remarks>
        Task ProvideFor(IProjection projection);

        /// <summary>
        /// Resumes if paused - continues providing events from this point.
        /// </summary>
        void Resume();

        /// <summary>
        /// Pause - ceases providing events from this point.
        /// </summary>
        void Pause();

        /// <summary>
        /// Rewind for a <see cref="IProjection"/>.
        /// </summary>
        /// <param name="projection"><see cref="IProjection"/> to rewind for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// Rewinding means setting the position within a stream of events back to the beginning of time.
        /// Once the ProvideFor() method is called or already is called for a projection - it should then
        /// start replaying.
        /// </remarks>
        Task Rewind(IProjection projection);
    }
}

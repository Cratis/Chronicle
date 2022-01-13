// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the states a <see cref="IProjection"/> can be in.
    /// </summary>
    public enum ProjectionState
    {
        /// <summary>
        /// Projection is in an unknown state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Projection is being registered.
        /// </summary>
        Registering = 1,

        /// <summary>
        /// Projection is in rewind mode for all result stores and catching up.
        /// </summary>
        Rewinding = 2,

        /// <summary>
        /// Projection is catching up due to being behind current sequence number in event log.
        /// </summary>
        CatchingUp = 3,

        /// <summary>
        /// Projection is active and waiting for new events.
        /// </summary>
        Active = 4,

        /// <summary>
        /// Projection is paused.
        /// </summary>
        Paused = 5,

        /// <summary>
        /// Projection is stopped.
        /// </summary>
        Stopped = 6,

        /// <summary>
        /// Projection is suspended, most likely due to failure.
        /// </summary>
        Suspended = 7
    }
}

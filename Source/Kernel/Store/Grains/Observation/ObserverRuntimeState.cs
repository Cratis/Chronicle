// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Defines the states a <see cref="IObserver"/> can be in.
/// </summary>
public enum ObserverRuntimeState
{
    /// <summary>
    /// Observer is in an unknown state.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Observer is being registered.
    /// </summary>
    Registering = 1,

    /// <summary>
    /// Observer is in rewind mode for all sinks and catching up.
    /// </summary>
    Rewinding = 2,

    /// <summary>
    /// Observer is catching up due to being behind current sequence number in event log.
    /// </summary>
    CatchingUp = 3,

    /// <summary>
    /// Observer is active and waiting for new events.
    /// </summary>
    Active = 4,

    /// <summary>
    /// Observer is paused.
    /// </summary>
    Paused = 5,

    /// <summary>
    /// Observer is stopped.
    /// </summary>
    Stopped = 6,

    /// <summary>
    /// Observer is suspended, most likely due to failure.
    /// </summary>
    Suspended = 7,
}

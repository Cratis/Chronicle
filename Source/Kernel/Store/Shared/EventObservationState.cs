// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Represents the observation state for an event.
/// </summary>
public enum EventObservationState
{
    /// <summary>
    /// The initial observation, first time being observed.
    /// </summary>
    Initial = 0,

    /// <summary>
    /// Replay observation, this is not the first time its observed for the observer.
    /// </summary>
    Replay = 1,

    /// <summary>
    /// The tail of the replay, the last event for the replay.
    /// </summary>
    TailOfReplay = 2,
}

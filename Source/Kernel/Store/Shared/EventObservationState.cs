// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Represents the observation state for an event.
/// </summary>
[Flags]
public enum EventObservationState
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The initial observation, first time being observed.
    /// </summary>
    Initial = 1,

    /// <summary>
    /// The head of the replay, the first event for the replay.
    /// </summary>
    HeadOfReplay = 1 << 1,

    /// <summary>
    /// Replay observation, this is not the first time its observed for the observer.
    /// </summary>
    Replay = 1 << 2,

    /// <summary>
    /// The tail of the replay, the last event for the replay.
    /// </summary>
    TailOfReplay = 1 << 3,
}

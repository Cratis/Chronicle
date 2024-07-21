// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Defines the status of an observer.
/// </summary>
public enum ObserverRunningState
{
    /// <summary>
    /// Observer is in an unknown state.
    /// </summary>
    New = 0,

    /// <summary>
    /// Observer is in the routing state. This is the state in which it will figure out what state it should be in.
    /// </summary>
    Routing = 1,

    /// <summary>
    /// Observer is replaying.
    /// </summary>
    Replaying = 2,

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
    /// Observer is suspended.
    /// </summary>
    Suspended = 7,

    /// <summary>
    /// Observer is failed.
    /// </summary>
    Failed = 8,

    /// <summary>
    /// Observer is at the tail of replay.
    /// </summary>
    TailOfReplay = 9,

    /// <summary>
    /// Observer is disconnected.
    /// </summary>
    Disconnected = 10,

    /// <summary>
    /// Observer is disconnected.
    /// </summary>
    Indexing = 11,
}

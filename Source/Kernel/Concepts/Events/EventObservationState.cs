// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

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
    /// Replay observation, this is not the first time its observed for the observer.
    /// </summary>
    Replay = 1 << 1,

    /// <summary>
    /// The observer is working through a backlog rather than observing as events happen.
    /// </summary>
    /// <remarks>
    /// Composes with <see cref="Initial"/>: catch-up events genuinely have not been seen by this observer
    /// before, so both flags are set. The distinction matters because 'this just happened' and 'this happened
    /// three days ago and the observer is only now reaching it' demand opposite things of a reactor that acts
    /// on the world - and they were the same value.
    /// </remarks>
    CatchUp = 1 << 2,
}

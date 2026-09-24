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
    /// Delivered on its own rather than combined with <see cref="Initial"/>, and it implies it: a catch-up
    /// event has not been observed before. The distinction matters because 'this just happened' and 'this
    /// happened three days ago and the observer is only now reaching it' demand opposite things of a reactor
    /// that acts on the world - and they used to be the same value.
    /// <para>
    /// Delivering it combined was tried first and stalls every catch-up observer. Despite the [Flags]
    /// declaration, a combination does not survive the round trip through job-step state and the wire, and
    /// the stall is silent - the observer simply never reaches running. Until that is fixed, only single
    /// declared members may be put on this enum in flight.
    /// </para>
    /// </remarks>
    CatchUp = 1 << 2,
}

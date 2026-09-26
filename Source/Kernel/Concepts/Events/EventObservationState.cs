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
    /// Delivering it combined stalls every catch-up observer on Cratis.Fundamentals 7.19.3 and earlier:
    /// Orleans routes every Cratis-namespaced type through Fundamentals' EnumConverter, which wrote a [Flags]
    /// combination and then refused to read it back, so the job step arguments failed to deserialize, the step
    /// never ran, and the observer silently never reached running. Fixed in Fundamentals by
    /// https://github.com/Cratis/Fundamentals/pull/1131; once that version is picked up here, this may be
    /// delivered as Initial | CatchUp and this note removed.
    /// </para>
    /// </remarks>
    CatchUp = 1 << 2,
}

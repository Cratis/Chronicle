// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents what an event sequence query is ordered by.
/// </summary>
/// <remarks>
/// Only values stored alongside the event itself can be ordered on, because the ordering has to
/// happen in storage - sorting a page after it arrives would only order that page.
/// </remarks>
public enum EventSequenceQuerySortBy
{
    /// <summary>
    /// The position of the event in the sequence - the order it was appended in.
    /// </summary>
    SequenceNumber = 0,

    /// <summary>
    /// When the event occurred.
    /// </summary>
    Occurred = 1,

    /// <summary>
    /// The type of the event.
    /// </summary>
    EventType = 2,

    /// <summary>
    /// The event source the event belongs to.
    /// </summary>
    EventSourceId = 3
}

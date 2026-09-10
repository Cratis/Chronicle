// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Represents what a saved event sequence query orders its results by.
/// </summary>
public enum SequenceQuerySortBy
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

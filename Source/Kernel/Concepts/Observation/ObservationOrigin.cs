// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the path a delivery of events to an observer came from.
/// </summary>
public enum ObservationOrigin
{
    /// <summary>
    /// The origin is not known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The events were appended now and delivered to an observer that is up to date.
    /// </summary>
    Live = 1,

    /// <summary>
    /// The events were read from storage for an observer that is behind.
    /// </summary>
    CatchUp = 2,

    /// <summary>
    /// The events were read from storage for an observer that was rewound.
    /// </summary>
    Replay = 3,

    /// <summary>
    /// The events were read from storage to recover a failed or deferred partition.
    /// </summary>
    Retry = 4
}

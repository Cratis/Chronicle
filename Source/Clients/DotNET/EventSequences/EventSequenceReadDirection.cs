// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The order in which events are read from an event sequence.
/// </summary>
public enum EventSequenceReadDirection
{
    /// <summary>
    /// Read oldest first, toward increasing sequence numbers.
    /// </summary>
    Forward = 0,

    /// <summary>
    /// Read newest first, toward decreasing sequence numbers.
    /// </summary>
    Backward = 1
}

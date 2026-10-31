// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes whether an observer partition can be rebuilt independently of other partitions.
/// </summary>
public enum PartitionReplayability
{
    /// <summary>
    /// The definition may depend on other partitions or contains an unrecognized shape.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Each partition depends only on its own event source.
    /// </summary>
    Closed = 1
}

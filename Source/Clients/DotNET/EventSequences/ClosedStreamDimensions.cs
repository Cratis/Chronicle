// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The dimensions participating in a closed stream scope.
/// </summary>
[Flags]
public enum ClosedStreamDimensions
{
    /// <summary>
    /// No dimensions.
    /// </summary>
    None = 0,

    /// <summary>
    /// The event source identifier.
    /// </summary>
    EventSourceId = 1,

    /// <summary>
    /// The event source type.
    /// </summary>
    EventSourceType = 2,

    /// <summary>
    /// The event stream type.
    /// </summary>
    EventStreamType = 4,

    /// <summary>
    /// The event stream identifier.
    /// </summary>
    EventStreamId = 8
}

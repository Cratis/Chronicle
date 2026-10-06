// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSources;

/// <summary>
/// Represents the dimensions that take part in a concurrency check.
/// </summary>
[Flags]
public enum ConcurrencyDimensions
{
    /// <summary>
    /// No dimensions are declared.
    /// </summary>
    None = 0,

    /// <summary>
    /// The event source id.
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
    /// The event stream id.
    /// </summary>
    EventStreamId = 8
}

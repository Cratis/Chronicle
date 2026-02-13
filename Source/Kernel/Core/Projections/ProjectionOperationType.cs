// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the type of operation an event will perform on a projection.
/// </summary>
[Flags]
public enum ProjectionOperationType
{
    /// <summary>
    /// No operation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Properties are set from the event.
    /// </summary>
    From = 1,

    /// <summary>
    /// Properties are joined from event.
    /// </summary>
    Join = 1 << 1,

    /// <summary>
    /// Entity is removed.
    /// </summary>
    Remove = 1 << 2,

    /// <summary>
    /// Children are affected.
    /// </summary>
    ChildrenAffected = 1 << 3
}

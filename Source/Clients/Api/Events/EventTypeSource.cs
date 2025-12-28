// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents the source of an event type.
/// </summary>
public enum EventTypeSource
{
    /// <summary>
    /// The source is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The event type is from the core system.
    /// </summary>
    Code = 1,

    /// <summary>
    /// The event type is from a user.
    /// </summary>
    User = 2
}

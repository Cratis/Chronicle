// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents the owner of an event type.
/// </summary>
public enum EventTypeOwner
{
    /// <summary>
    /// The event type is owned by no one.
    /// </summary>
    None = 0,

    /// <summary>
    /// The event type is owned by the client.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The event type is owned by the server.
    /// </summary>
    Server = 2
}

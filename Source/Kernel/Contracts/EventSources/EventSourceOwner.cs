// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSources;

/// <summary>
/// Represents who owns an event source definition.
/// </summary>
public enum EventSourceOwner
{
    /// <summary>
    /// Owned by no one.
    /// </summary>
    None = 0,

    /// <summary>
    /// Owned by the client that registered it.
    /// </summary>
    Client = 1,

    /// <summary>
    /// Owned by the Kernel.
    /// </summary>
    Kernel = 2
}

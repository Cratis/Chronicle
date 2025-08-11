// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the owner of an observer.
/// </summary>
public enum ObserverOwner
{
    /// <summary>
    /// The observer has no specific owner.
    /// </summary>
    None = 0,

    /// <summary>
    /// The observer is owned by a client.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The observer is owned by the kernel.
    /// </summary>
    Kernel = 2
}

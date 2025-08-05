// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Reactors;

/// <summary>
/// Represents the owner of a reactor.
/// </summary>
public enum ReactorOwner
{
    /// <summary>
    /// The reactor has no specific owner.
    /// </summary>
    None = 0,

    /// <summary>
    /// The reactor is owned by a client.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The reactor is owned by the kernel.
    /// </summary>
    Kernel = 2
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the owner of a read model.
/// </summary>
public enum ReadModelOwner
{
    /// <summary>
    /// The read model is owned by no one.
    /// </summary>
    None = 0,

    /// <summary>
    /// The read model is owned by the client.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The read model is owned by the kernel.
    /// </summary>
    Kernel = 2
}

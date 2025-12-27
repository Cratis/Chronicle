// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

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
    /// The read model is owned by the server.
    /// </summary>
    Server = 2,

    /// <summary>
    /// The read model is owned by the workbench.
    /// </summary>
    Workbench = 3
}

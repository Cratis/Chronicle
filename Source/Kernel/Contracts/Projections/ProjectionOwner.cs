// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the owner of a projection.
/// </summary>
public enum ProjectionOwner
{
    /// <summary>
    /// The projection is owned by no one.
    /// </summary>
    None = 0,

    /// <summary>
    /// The projection is owned by the client.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The projection is owned by the server.
    /// </summary>
    Server = 2,

    /// <summary>
    /// The projection is owned by a parent projection.
    /// </summary>
    Parent = 3,

    /// <summary>
    /// The projection is owned by the kernel itself.
    /// </summary>
    /// <remarks>
    /// Carried on the wire so a consumer can tell a system projection apart from one a client registered - the
    /// Workbench uses it to keep replay and retirement off a projection the kernel owns.
    /// </remarks>
    Kernel = 4
}

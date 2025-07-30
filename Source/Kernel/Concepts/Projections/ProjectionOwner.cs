// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections;

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
    Parent = 3
}

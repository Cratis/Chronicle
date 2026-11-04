// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Identifies one generation of a connection.
/// </summary>
/// <param name="owner">The principal opening the connection.</param>
public sealed class ConnectionClaim(ConnectionOwner owner)
{
    /// <summary>
    /// Gets the principal opening the connection.
    /// </summary>
    public ConnectionOwner Owner { get; } = owner;
}

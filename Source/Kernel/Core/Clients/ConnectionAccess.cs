// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Describes access to a connection on this silo.
/// </summary>
public enum ConnectionAccess
{
    /// <summary>
    /// The caller may use the connection.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// The connection is not owned on this silo.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The caller may not use the connection.
    /// </summary>
    Denied = 2
}

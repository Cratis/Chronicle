// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents the authentication mode for Chronicle connections.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Client credentials authentication.
    /// </summary>
    ClientCredentials = 0,

    /// <summary>
    /// API key authentication.
    /// </summary>
    ApiKey = 1,

    /// <summary>
    /// No authentication - the client presents no credentials at all.
    /// </summary>
    /// <remarks>
    /// Only usable against a server running with authentication turned off
    /// (<c>Cratis:Chronicle:Authentication:Enabled=false</c>), which is meant for a Chronicle embedded in the
    /// same container or process as its client. Against any server that enforces authentication, every call
    /// fails as unauthenticated.
    /// </remarks>
    None = 2,
}

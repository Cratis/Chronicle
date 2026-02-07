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
}

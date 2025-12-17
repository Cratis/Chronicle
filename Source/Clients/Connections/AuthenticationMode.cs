// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents the authentication mode for Chronicle connections.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// No authentication.
    /// </summary>
    None = 0,

    /// <summary>
    /// Internal user/password authentication.
    /// </summary>
    InternalUserPassword = 1,

    /// <summary>
    /// API key authentication.
    /// </summary>
    ApiKey = 2,

    /// <summary>
    /// OpenID Connect (OIDC) authentication.
    /// </summary>
    Oidc = 3
}

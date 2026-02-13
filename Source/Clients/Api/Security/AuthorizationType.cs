// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents an authorization type.
/// </summary>
public enum AuthorizationType
{
    /// <summary>
    /// No authorization.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic authentication.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Bearer token authentication.
    /// </summary>
    Bearer = 2,

    /// <summary>
    /// OAuth authentication.
    /// </summary>
    OAuth = 3
}

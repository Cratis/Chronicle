// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents which kind of authorization an outbound HTTP call carries.
/// </summary>
/// <remarks>
/// The authorization itself travels as a <c>OneOf</c> of the concrete configurations, which says what the
/// authorization <em>is</em> but not what a caller <em>intends</em> before it has filled anything in. This is the
/// discriminator a form selects on.
/// </remarks>
public enum AuthorizationType
{
    /// <summary>
    /// No authorization.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic authorization with a username and password.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Bearer token authorization.
    /// </summary>
    Bearer = 2,

    /// <summary>
    /// OAuth client credentials authorization.
    /// </summary>
    OAuth = 3
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Why an authorization decision came out as it did. The default is <see cref="Failed"/>. Values are stable and never renumbered.
/// </summary>
public enum AuthorizationReason
{
    /// <summary>
    /// The decision could not be established.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// The principal holds the capability.
    /// </summary>
    Granted = 1,

    /// <summary>
    /// Authentication is disabled, so everything is allowed.
    /// </summary>
    AuthenticationDisabled = 2,

    /// <summary>
    /// The capability is not yet enforced at this scope.
    /// </summary>
    NotEnrolled = 3,

    /// <summary>
    /// There is no authenticated principal.
    /// </summary>
    NoPrincipal = 4,

    /// <summary>
    /// The principal does not hold the capability.
    /// </summary>
    NotGranted = 5,

    /// <summary>
    /// The capability is not known to the kernel.
    /// </summary>
    UnknownCapability = 6
}

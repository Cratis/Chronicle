// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The outcome of an authorization decision. The default is <see cref="Denied"/>. Values are stable and never renumbered.
/// </summary>
public enum AuthorizationOutcome
{
    /// <summary>
    /// The request is denied.
    /// </summary>
    Denied = 0,

    /// <summary>
    /// The request is allowed.
    /// </summary>
    Allowed = 1
}

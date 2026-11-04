// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The single point deciding whether a principal holds a capability in a scope. It fails closed.
/// </summary>
public interface IAuthorizationDecisions
{
    /// <summary>
    /// Decides whether the principal holds the capability in the scope.
    /// </summary>
    /// <param name="principal">The caller, or null when there is none.</param>
    /// <param name="capability">The capability asked for.</param>
    /// <param name="scope">The scope it is asked for in.</param>
    /// <returns>The decision; it never throws.</returns>
    Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope);
}

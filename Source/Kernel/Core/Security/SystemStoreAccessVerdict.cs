// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Allows access to the System event store only to those the authorization decision point permits.
/// </summary>
/// <param name="decisions">The authorization decision point.</param>
[Singleton]
public class SystemStoreAccessVerdict(IAuthorizationDecisions decisions) : ISystemStoreAccess
{
    /// <inheritdoc/>
    public async Task<bool> IsAllowed(ClaimsPrincipal? principal) =>
        (await decisions.Decide(principal, WellKnownCapabilities.AccessSystemStore, AuthorizationScope.ForEventStore(EventStoreName.System))).IsAllowed;

    /// <summary>Checks access using the caller's provider.</summary>
    /// <param name="principal">The caller.</param>
    /// <param name="services">The caller's provider.</param>
    /// <returns>Whether access is allowed.</returns>
    internal async Task<bool> IsAllowed(ClaimsPrincipal? principal, IServiceProvider? services) =>
        (await decisions.Decide(new AuthorizationCaller(principal, services), WellKnownCapabilities.AccessSystemStore, AuthorizationScope.ForEventStore(EventStoreName.System))).IsAllowed;
}

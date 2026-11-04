// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Allows access to the System event store only to those the authorization decision point permits.
/// </summary>
/// <param name="decisions">The authorization decision point.</param>
public class SystemStoreAccessVerdict(IAuthorizationDecisions decisions) : ISystemStoreAccess
{
    /// <inheritdoc/>
    public async Task<bool> IsAllowed(ClaimsPrincipal? principal) =>
        (await decisions.Decide(principal, WellKnownCapabilities.AccessSystemStore, AuthorizationScope.ForEventStore(EventStoreName.System))).IsAllowed;
}

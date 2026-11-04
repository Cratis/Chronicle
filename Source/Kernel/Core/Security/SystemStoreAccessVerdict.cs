// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Allows access to the System event store only to administrators.
/// </summary>
/// <param name="authorization">The administrator check.</param>
public class SystemStoreAccessVerdict(IAdministratorAuthorization authorization) : ISystemStoreAccess
{
    /// <inheritdoc/>
    public Task<bool> IsAllowed(ClaimsPrincipal? principal) =>
        authorization.IsAuthorized(principal ?? new ClaimsPrincipal(new ClaimsIdentity()));
}

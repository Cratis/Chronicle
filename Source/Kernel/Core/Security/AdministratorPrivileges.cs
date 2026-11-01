// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.DependencyInjection;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents authoritative administrator privilege checks.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
[Singleton]
public class AdministratorPrivileges(IGrainFactory grainFactory) : IAdministratorPrivileges
{
    /// <inheritdoc/>
    public Task<bool> IsActiveAdministrator(UserId userId) =>
        grainFactory.GetGrain<IAdministratorRoster>(Roster.Key).IsActiveAdministrator(userId);
}

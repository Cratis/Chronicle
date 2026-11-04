// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// An <see cref="IAdministratorAuthorization"/> that counts how often it is asked.
/// </summary>
/// <param name="verdict">The verdict to return.</param>
public sealed class SystemStoreAuthorizationProbe(bool verdict) : IAdministratorAuthorization
{
    /// <summary>
    /// Gets the number of times the verdict was requested.
    /// </summary>
    public int Calls { get; private set; }

    /// <summary>
    /// Resets the call count.
    /// </summary>
    public void Reset() => Calls = 0;

    /// <inheritdoc/>
    public Task<bool> IsAuthorized(ClaimsPrincipal principal)
    {
        Calls++;
        return Task.FromResult(verdict);
    }
}

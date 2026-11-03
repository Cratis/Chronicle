// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Defines the verdict on whether a caller may read or operate on the System event store.
/// </summary>
/// <remarks>
/// Only consult this after <see cref="SystemStoreAccess.IsSystem"/> is true, so other event stores never pay for it.
/// </remarks>
public interface ISystemStoreAccess
{
    /// <summary>
    /// Checks whether a caller may access the System event store.
    /// </summary>
    /// <param name="principal">The caller, or null when there is no caller identity, which is treated as anonymous.</param>
    /// <returns>True if the caller is an administrator; false otherwise.</returns>
    Task<bool> IsAllowed(ClaimsPrincipal? principal);
}

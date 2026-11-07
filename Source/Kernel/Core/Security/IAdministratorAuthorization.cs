// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Checks the authenticated caller against the authoritative administrator roster.
/// </summary>
/// <remarks>
/// The caller is always passed in. Hosts may register a singleton, scoped, or transient implementation before or after
/// calling <see cref="AdministratorAuthorizationServiceCollectionExtensions.AddChronicleAdministratorAuthorization"/>.
/// The check is resolved for each administrator decision from the caller's service provider; dependent services remain singleton.
/// Without a caller provider, administrator decisions fail closed. Provider-less command execution creates a fresh scope,
/// with no request-scoped state; implementations requiring that state must refuse access when it is absent.
/// </remarks>
public interface IAdministratorAuthorization
{
    /// <summary>
    /// Checks administrator authority, failing closed when it cannot be established.
    /// </summary>
    /// <param name="principal">The caller.</param>
    /// <returns>Whether the caller may administer Chronicle.</returns>
    Task<bool> IsAuthorized(ClaimsPrincipal principal);
}

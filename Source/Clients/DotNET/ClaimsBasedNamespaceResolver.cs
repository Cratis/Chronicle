// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceResolver"/> that resolves the namespace from the current user's claims.
/// </summary>
/// <remarks>
/// This resolver accesses the current ClaimsPrincipal via Thread.CurrentPrincipal to resolve the namespace.
/// It is useful in scenarios where you need to resolve namespaces based on user identity without requiring HTTP context.
/// </remarks>
/// <param name="claimType">The claim type to use for resolving the namespace. Defaults to "tenant_id".</param>
public class ClaimsBasedNamespaceResolver(string claimType = "tenant_id") : IEventStoreNamespaceResolver
{
    readonly string _claimType = claimType;

    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve()
    {
        var principal = ClaimsPrincipal.Current;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var claim = principal.FindFirst(_claimType);
            if (claim is not null && !string.IsNullOrEmpty(claim.Value))
            {
                return claim.Value;
            }
        }

        return EventStoreNamespaceName.Default;
    }
}

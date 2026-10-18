// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceResolver"/> that resolves the namespace from the current user's claims.
/// </summary>
/// <remarks>
/// By default this resolver reads the current principal from <see cref="ClaimsPrincipal.Current"/> to resolve the namespace.
/// It is useful in scenarios where you need to resolve namespaces based on user identity without requiring HTTP context.
/// Supply a principal accessor to read the principal from somewhere else.
/// </remarks>
/// <param name="currentPrincipal">Callback that returns the current <see cref="ClaimsPrincipal"/>, or null when there is none.</param>
/// <param name="claimType">The claim type to use for resolving the namespace. Defaults to "tenant_id".</param>
public class ClaimsBasedNamespaceResolver(Func<ClaimsPrincipal?> currentPrincipal, string claimType = "tenant_id") : IEventStoreNamespaceResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimsBasedNamespaceResolver"/> class that reads the principal from <see cref="ClaimsPrincipal.Current"/>.
    /// </summary>
    /// <param name="claimType">The claim type to use for resolving the namespace. Defaults to "tenant_id".</param>
    public ClaimsBasedNamespaceResolver(string claimType = "tenant_id")
        : this(() => ClaimsPrincipal.Current, claimType)
    {
    }

    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve()
    {
        var principal = currentPrincipal();
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var claim = principal.FindFirst(claimType);
            if (claim is not null && !string.IsNullOrEmpty(claim.Value))
            {
                return claim.Value;
            }
        }

        return EventStoreNamespaceName.Default;
    }
}

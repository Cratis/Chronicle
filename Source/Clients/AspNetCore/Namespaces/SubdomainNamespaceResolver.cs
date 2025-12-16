// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceResolver"/> that resolves the namespace from the subdomain of the HTTP request host.
/// </summary>
/// <remarks>
/// This resolver extracts the subdomain from the request host (e.g., "tenant1" from "tenant1.example.com").
/// It is useful for multi-tenant scenarios where tenants are identified by subdomain.
/// </remarks>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> for accessing the current HTTP context.</param>
public class SubdomainNamespaceResolver(IHttpContextAccessor httpContextAccessor) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve()
    {
        var host = httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            return EventStoreNamespaceName.Default;
        }

        // Extract subdomain (e.g., "customer123" from "customer123.example.com")
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            return parts[0];
        }

        return EventStoreNamespaceName.Default;
    }
}

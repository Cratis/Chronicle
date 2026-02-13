// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Namespaces;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="ChronicleAspNetCoreOptions"/>.
/// </summary>
public static class ChronicleAspNetCoreOptionsExtensions
{
    /// <summary>
    /// Configures the namespace resolver to use HTTP header-based resolution.
    /// </summary>
    /// <param name="options"><see cref="ChronicleAspNetCoreOptions"/> to configure.</param>
    /// <param name="headerName">The HTTP header name to use for resolving the namespace. Defaults to "x-cratis-tenant-id".</param>
    /// <returns>The same <see cref="ChronicleAspNetCoreOptions"/> instance with the HTTP header namespace resolver configured.</returns>
    public static ChronicleAspNetCoreOptions WithHttpHeaderNamespaceResolver(this ChronicleAspNetCoreOptions options, string headerName = "x-cratis-tenant-id")
    {
        options.NamespaceHttpHeader = headerName;
        options.EventStoreNamespaceResolverType = typeof(HttpHeaderEventStoreNamespaceResolver);
        return options;
    }

    /// <summary>
    /// Configures the namespace resolver to use subdomain-based resolution from the HTTP request host.
    /// </summary>
    /// <param name="options"><see cref="ChronicleAspNetCoreOptions"/> to configure.</param>
    /// <returns>The same <see cref="ChronicleAspNetCoreOptions"/> instance with the subdomain namespace resolver configured.</returns>
    public static ChronicleAspNetCoreOptions WithSubdomainNamespaceResolver(this ChronicleAspNetCoreOptions options)
    {
        options.EventStoreNamespaceResolverType = typeof(SubdomainNamespaceResolver);
        return options;
    }
}

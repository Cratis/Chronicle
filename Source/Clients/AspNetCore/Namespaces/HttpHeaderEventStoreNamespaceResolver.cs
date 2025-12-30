// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceResolver"/> that resolves the namespace from an HTTP header.
/// </summary>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> for accessing the current HTTP context.</param>
/// <param name="options">The <see cref="Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions"/> containing the header name configuration.</param>
public class HttpHeaderEventStoreNamespaceResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptions<Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions> options) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve()
    {
        var headerName = options.Value.NamespaceHttpHeader;
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(headerName, out var values) ?? false)
        {
            return values.ToString();
        }

        return EventStoreNamespaceName.Default;
    }
}

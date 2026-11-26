// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceResolver"/> that resolves the namespace from an HTTP header.
/// </summary>
/// <remarks>
/// A header that is sent more than once, or whose value is a comma-separated list, resolves to its single namespace when
/// every value is the same; it never becomes a comma-joined namespace name. Different values are ambiguous and rejected.
/// </remarks>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> for accessing the current HTTP context.</param>
/// <param name="options">The <see cref="Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions"/> containing the header name configuration.</param>
public class HttpHeaderEventStoreNamespaceResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptions<Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions> options) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    /// <exception cref="AmbiguousNamespaceHttpHeader">The header carries more than one distinct namespace.</exception>
    public EventStoreNamespaceName Resolve()
    {
        var headerName = options.Value.NamespaceHttpHeader;
        if (!(httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(headerName, out var values) ?? false))
        {
            return EventStoreNamespaceName.Default;
        }

        var namespaces = values
            .SelectMany(_ => (_ ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return namespaces.Length switch
        {
            0 => EventStoreNamespaceName.Default,
            1 => namespaces[0],
            _ => throw new AmbiguousNamespaceHttpHeader(headerName, namespaces)
        };
    }
}

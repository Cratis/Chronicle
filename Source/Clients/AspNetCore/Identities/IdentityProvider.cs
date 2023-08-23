// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;
using Microsoft.AspNetCore.Http;

namespace Aksio.Cratis.AspNetCore.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityProvider"/> for ASP.NET.
/// </summary>
public class IdentityProvider : BaseIdentityProvider
{
    readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor"><see cref="IHttpContextAccessor"/> for accessing the current <see cref="HttpContext"/>.</param>
    public IdentityProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public override Identity GetCurrent()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is null || context.Request.Path.StartsWithSegments("/.cratis")) return base.GetCurrent();

        var subject = context.User.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
        var name = context.User.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? string.Empty;
        var username = context.User.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? string.Empty;

        return new Identity(subject, name, username);
    }
}

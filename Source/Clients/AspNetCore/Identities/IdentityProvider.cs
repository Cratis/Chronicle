// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;
using Cratis.Chronicle.Identities;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Identities;

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
    protected override Identity GetCurrent()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context?.Request.Path.StartsWithSegments("/.cratis") ?? true) return base.GetCurrent();

        var subject = context.User.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
        var name = context.User.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? string.Empty;
        var username = context.User.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(subject) &&
            string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(username))
        {
            return Identity.NotSet;
        }

        return new Identity(subject, name, username);
    }
}

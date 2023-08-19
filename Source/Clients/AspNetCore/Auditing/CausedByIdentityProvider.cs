// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Microsoft.AspNetCore.Http;

namespace Aksio.Cratis.AspNetCore.Auditing;

/// <summary>
/// Represents an implementation of <see cref="ICausedByIdentityProvider"/> for ASP.NET.
/// </summary>
public class CausedByIdentityProvider : ICausedByIdentityProvider
{
    readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CausedByIdentityProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor"><see cref="IHttpContextAccessor"/> for accessing the current <see cref="HttpContext"/>.</param>
    public CausedByIdentityProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public CausedBy GetCurrent()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return CausedBy.NotSet;

        var subject = context.User.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
        var name = context.User.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? string.Empty;
        var username = context.User.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? string.Empty;

        return new CausedBy(subject, name, username);
    }
}

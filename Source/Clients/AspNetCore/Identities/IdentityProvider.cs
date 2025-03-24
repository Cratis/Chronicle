// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityProvider"/> for ASP.NET.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IdentityProvider"/> class.
/// </remarks>
/// <param name="httpContextAccessor"><see cref="IHttpContextAccessor"/> for accessing the current <see cref="HttpContext"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class IdentityProvider(IHttpContextAccessor httpContextAccessor, ILogger<IdentityProvider> logger) : BaseIdentityProvider
{
    /// <inheritdoc/>
    protected override Identity GetCurrent()
    {
        var context = httpContextAccessor.HttpContext;

        if (context?.Request.Path.StartsWithSegments("/.cratis") ?? true) return base.GetCurrent();

        var subject = context.User.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
        var name = context.User.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? context.User.Identity?.Name ?? string.Empty;
        var username = context.User.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? context.User.Identity?.Name ?? string.Empty;

        if (string.IsNullOrEmpty(subject) &&
            string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(username))
        {
            logger.IdentityNotSet();
            return Identity.NotSet;
        }

        logger.IdentitySet(subject, name, username);
        return new Identity(subject, name, username);
    }
}

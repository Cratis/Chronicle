// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Middleware that flows user identity from HTTP context to Orleans RequestContext.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
public class UserIdentityMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>Awaitable task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated ?? false)
        {
            var subject = context.User.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
            var name = context.User.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? context.User.Identity?.Name ?? string.Empty;
            var username = context.User.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? context.User.Identity?.Name ?? string.Empty;

            if (string.IsNullOrEmpty(subject) &&
                string.IsNullOrEmpty(name) &&
                string.IsNullOrEmpty(username))
            {
                return;
            }

            RequestContext.Set(WellKnownKeys.UserIdentity, subject);
            RequestContext.Set(WellKnownKeys.UserName, name);
            RequestContext.Set(WellKnownKeys.UserPreferredUserName, username);
        }

        await next(context);
    }
}
